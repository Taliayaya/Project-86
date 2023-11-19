using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class CutsceneManager : Singleton<CutsceneManager>
{
    private static List<int> _playedCutscenes = new List<int>();
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void PlayCutScene(PlayableDirector director, PlayableAsset playableAsset, int cutsceneId)
    {
        if (_playedCutscenes.Contains(cutsceneId)) return;
        _playedCutscenes.Add(cutsceneId);
        director.playableAsset = playableAsset;
        director.Play();
    }
    
    public static void AddPlayedCutscene(int cutsceneId)
    {
        _playedCutscenes.Add(cutsceneId);
    }
    
    public static bool HasPlayedCutscene(int cutsceneId)
    {
        return _playedCutscenes.Contains(cutsceneId);
    }
}
