#import <Foundation/Foundation.h>
#import <AVFoundation/AVFoundation.h>

static void (*gSuspendCallback)(bool suspend) = nullptr;
static bool gIsSuspended = false;
static bool gNeedsReset = false;

extern "C" void RegisterSuspendCallback(void (*callback)(bool))
{
    if (gSuspendCallback || !callback)
    {
        return;
    }
    gSuspendCallback = callback;

    [[NSNotificationCenter defaultCenter] addObserverForName:AVAudioSessionInterruptionNotification object:nil queue:nil usingBlock:^(NSNotification *notification)
    {
        AVAudioSessionInterruptionType type = (AVAudioSessionInterruptionType)[[notification.userInfo valueForKey:AVAudioSessionInterruptionTypeKey] unsignedIntegerValue];
        if (type == AVAudioSessionInterruptionTypeBegan)
        {
            NSLog(@"Interruption Began");
            // Ignore deprecated warnings regarding AVAudioSessionInterruptionReasonAppWasSuspended and
            // AVAudioSessionInterruptionWasSuspendedKey, we protect usage for the versions where they are available
            #pragma clang diagnostic push
            #pragma clang diagnostic ignored "-Wdeprecated-declarations"

            // If the audio session was deactivated while the app was in the background, the app receives the
            // notification when relaunched. Identify this reason for interruption and ignore it.
            if (@available(iOS 16.0, tvOS 14.5, *))
            {
                // Delayed suspend-in-background notifications no longer exist, this must be a real interruption
            }
            #if !TARGET_OS_TV // tvOS never supported "AVAudioSessionInterruptionReasonAppWasSuspended"
            else if (@available(iOS 14.5, *))
            {
                if ([[notification.userInfo valueForKey:AVAudioSessionInterruptionReasonKey] intValue] == AVAudioSessionInterruptionReasonAppWasSuspended)
                {
                    return; // Ignore delayed suspend-in-background notification
                }
            }
            #endif
            else
            {
                if ([[notification.userInfo valueForKey:AVAudioSessionInterruptionWasSuspendedKey] boolValue])
                {
                    return; // Ignore delayed suspend-in-background notification
                }
            }

            gSuspendCallback(true);
            gIsSuspended = true;

            #pragma clang diagnostic pop
        }
        else if (type == AVAudioSessionInterruptionTypeEnded)
        {
            NSLog(@"Interruption Ended");
            NSError *errorMessage = nullptr;
            if (![[AVAudioSession sharedInstance] setActive:TRUE error:&errorMessage])
            {
                // Interruption like Siri can prevent session activation, wait for did-become-active notification
                NSLog(@"AVAudioSessionInterruptionNotification: AVAudioSession.setActive() failed: %@", errorMessage);
                return;
            }

            gSuspendCallback(false);
            gIsSuspended = false;
        }
    }];

    [[NSNotificationCenter defaultCenter] addObserverForName:UIApplicationDidBecomeActiveNotification object:nil queue:nil usingBlock:^(NSNotification *notification)
    {
        // Unity video playback prior to 2022.3 on tvOS breaks FMOD audio, so force a reset
        #if TARGET_OS_TV && !UNITY_2022_3_OR_NEWER
        gNeedsReset = true;
        #endif

        if (gNeedsReset)
        {
            gSuspendCallback(true);
            gIsSuspended = true;
        }
        
        NSError *errorMessage = nullptr;
        if (![[AVAudioSession sharedInstance] setActive:TRUE error:&errorMessage])
        {
            if ([errorMessage code] == AVAudioSessionErrorCodeCannotStartPlaying)
            {
                // Interruption like Screen Time can prevent session activation, but will not trigger an interruption-ended notification.
                // There is no other callback or trigger to hook into after this point, we are not in the background and there is no other audio playing.
                // Our only option is to have a sleep loop until the Audio Session can be activated again.
                while (![[AVAudioSession sharedInstance] setActive:TRUE error:nil])
                {
                    usleep(20000);
                }
            }
            else
            {
                // Interruption like Siri can prevent session activation, wait for interruption-ended notification.
                NSLog(@"UIApplicationDidBecomeActiveNotification: AVAudioSession.setActive() failed: %@", errorMessage);
                return;
            }
        }

        // It's possible the system missed sending us an interruption end, so recover here
        if (gIsSuspended)
        {
            gSuspendCallback(false);
            gNeedsReset = false;
            gIsSuspended = false;
        }
    }];

    [[NSNotificationCenter defaultCenter] addObserverForName:AVAudioSessionMediaServicesWereResetNotification object:nil queue:nil usingBlock:^(NSNotification *notification)
    {
        if ([UIApplication sharedApplication].applicationState == UIApplicationStateBackground || gIsSuspended)
        {
            // Received the reset notification while in the background, need to reset the AudioUnit when we come back to foreground.
            gNeedsReset = true;
        }
        else
        {
            // In the foregound but something chopped the media services, need to do a reset.
            gSuspendCallback(true);
            gSuspendCallback(false);
        }
    }];
}
