void MainLight_half(float3 WorldPos, out half3 Direction, out half3 Color, out half DistanceAtten, out half ShadowAtten)
{
#ifdef SHADERGRAPH_PREVIEW
	Direction = half3(0.5, 0.5, 0);
	Color = 1;
	DistanceAtten = 1;
	ShadowAtten = 1;
#else
	half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
	Light mainLight = GetMainLight(shadowCoord);
	Direction = mainLight.direction;
	Color = mainLight.color;
	DistanceAtten = mainLight.distanceAttenuation;
	
#if !defined(_MAIN_LIGHT_SHADOWS) || defined(_RECEIVE_SHADOWS_OFF)
	ShadowAtten = 1.0h;
#endif
	ShadowAtten = mainLight.shadowAttenuation;
#endif
}


void AdditionalLights_half(half3 WorldPos, half3 WorldNormal, half3 WorldView, out half3 Diffuse)
{
	half3 diffuseColor = 0;

#ifndef SHADERGRAPH_PREVIEW
	WorldNormal = normalize(WorldNormal);
	WorldView = SafeNormalize(WorldView);
	int pixelLightCount = GetAdditionalLightsCount();
	for (int i = 0; i < pixelLightCount; ++i)
	{
		Light light = GetAdditionalLight(i, WorldPos);
		half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
		diffuseColor += LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
	}
#endif

	Diffuse = diffuseColor;
}



void ReflectionUV_half(half2 screenUV, half3 normalWS, half3 posWS, out half2 reflectionUV)
{

	const half f = 0.02f;
	half3 viewVec = _WorldSpaceCameraPos - posWS;
	half3 reflNormalWS = lerp(normalWS, normalize(float3(normalWS.x * f, 1.f, normalWS.z * f)), saturate(length(viewVec.xz) / 250.f));
	// get the perspective projection
	float2 p11_22 = float2(unity_CameraInvProjection._11, unity_CameraInvProjection._22) * 10;
	// conver the uvs into view space by "undoing" projection
	float3 viewDir = -(float3((screenUV * 2 - 1) / p11_22, -1));

	half3 viewNormal = mul(reflNormalWS, (float3x3)GetWorldToViewMatrix()).xyz;
	half3 reflectVector = reflect(-viewDir, viewNormal);

	reflectionUV = screenUV + reflNormalWS.zx * half2(0.05, 0.05);
}