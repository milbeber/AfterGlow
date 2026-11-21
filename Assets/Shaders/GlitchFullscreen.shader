// Полноэкранный глитч AfterGlow.
// Рисуется через GlitchRendererFeature, параметры приходят из оверрайда «AfterGlow/Glitch» в Volume-профиле.
//
// Атмосфера: розовое кружево «Академии «Астра»», которое гниёт изнутри.
// Все цвета сбоя берутся из палитры игры (пустота → кровь → алая кровь → неоновый розовый → бледно-розовый,
// плюс голубая тень сбоя), поэтому даже сплошной датамош остаётся «своим», а не радужным.
//
// Слои сбоя:
//  1) горизонтальные полосы со сдвигом; RGB расходится в розовую и голубую кайму;
//  2) «битое видео»: макроблоки копируются не на место, пикселизуются, стекают вниз
//     кровавыми потёками, тянутся вбок, застревают в прошлом кадре (датамош) или
//     перекрашиваются в неоновую палитру;
//  3) порча цвета: кровь проступает сквозь картинку, розовый негатив, голубой призрак, постеризация;
//  4) тонкие строки шума в цветах палитры;
//  5) при сильном сбое весь кадр тонет в винно-розовом.
// Внизу экрана защищённая зона (окно диалога): там сбой слабее, чтобы текст читался.
Shader "AfterGlow/GlitchFullscreen"
{
    Properties
    {
        _Intensity ("Intensity", Range(0, 1)) = 0
        _BlockCount ("Band Count", Range(4, 64)) = 24
        _ColorSplit ("Color Split", Range(0, 0.03)) = 0.004
        _MaxShift ("Max Shift", Range(0, 0.2)) = 0.05
        _Speed ("Speed", Range(0, 30)) = 10
        _Corruption ("Corruption", Range(0, 1)) = 0.7
        _MacroBlocks ("Macro Block Rows", Range(4, 40)) = 12
        _ColorCrush ("Color Crush", Range(0, 1)) = 0.6
        _Datamosh ("Datamosh", Range(0, 1)) = 0.6
        _Drown ("Drown In Palette", Range(0, 1)) = 0.35
        _SafeZoneHeight ("Safe Zone Height", Range(0, 0.6)) = 0.33
        _SafeZoneStrength ("Safe Zone Strength", Range(0, 1)) = 0.8
        _SafeZoneFlip ("Safe Zone Flip", Float) = 0
        _HistoryValid ("History Valid", Float) = 0

        _VoidColor ("Void", Color) = (0.078, 0.020, 0.031, 1)
        _BloodColor ("Blood", Color) = (0.557, 0.059, 0.114, 1)
        _BloodBrightColor ("Blood Bright", Color) = (0.886, 0.149, 0.231, 1)
        _NeonColor ("Neon Pink", Color) = (1.0, 0.310, 0.639, 1)
        _PaleColor ("Pale Pink", Color) = (1.0, 0.890, 0.945, 1)
        _AccentColor ("Glitch Cyan", Color) = (0.373, 0.890, 0.949, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "AfterGlowGlitch"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_GlitchHistoryTex);

            float _Intensity;
            float _BlockCount;
            float _ColorSplit;
            float _MaxShift;
            float _Speed;
            float _Corruption;
            float _MacroBlocks;
            float _ColorCrush;
            float _Datamosh;
            float _Drown;
            float _SafeZoneHeight;
            float _SafeZoneStrength;
            float _SafeZoneFlip;
            float _HistoryValid;

            float4 _VoidColor;
            float4 _BloodColor;
            float4 _BloodBrightColor;
            float4 _NeonColor;
            float4 _PaleColor;
            float4 _AccentColor;

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            half3 SampleScreen(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, saturate(uv), 0).rgb;
            }

            half3 SampleHistory(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X_LOD(_GlitchHistoryTex, sampler_LinearClamp, saturate(uv), 0).rgb;
            }

            // Воспринимаемая яркость 0..1 (цвета в линейном пространстве, поэтому берём корень)
            float Brightness(half3 c)
            {
                return sqrt(saturate(dot(c, half3(0.2126, 0.7152, 0.0722))));
            }

            // Градиент палитры игры: пустота → кровь → алая кровь → неон → бледно-розовый
            half3 PaletteMap(float t)
            {
                t = saturate(t);
                half3 c = lerp(_VoidColor.rgb, _BloodColor.rgb, saturate(t / 0.3));
                c = lerp(c, _BloodBrightColor.rgb, saturate((t - 0.3) / 0.25));
                c = lerp(c, _NeonColor.rgb, saturate((t - 0.55) / 0.25));
                c = lerp(c, _PaleColor.rgb, saturate((t - 0.8) / 0.2));
                return c;
            }

            // Только кровавая часть палитры
            half3 BloodMap(float t)
            {
                t = saturate(t);
                half3 c = lerp(_VoidColor.rgb, _BloodColor.rgb, saturate(t / 0.45));
                return lerp(c, _BloodBrightColor.rgb, saturate((t - 0.45) / 0.55));
            }

            half3 PickNoiseColor(float h)
            {
                if (h < 0.4) return _NeonColor.rgb;
                if (h < 0.65) return _AccentColor.rgb;
                if (h < 0.85) return _BloodBrightColor.rgb;
                return _PaleColor.rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float tick = floor(_Time.y * _Speed);
                float slowTick = floor(_Time.y * max(_Speed * 0.35, 0.5));

                // ---------- Защищённая зона (окно диалога внизу) ----------
                float zoneY = _SafeZoneFlip > 0.5 ? 1.0 - uv.y : uv.y;
                float inZone = (1.0 - smoothstep(_SafeZoneHeight - 0.03, _SafeZoneHeight + 0.03, zoneY))
                             * step(0.001, _SafeZoneHeight);
                float protect = 1.0 - inZone * _SafeZoneStrength;

                float intensity = _Intensity * protect;
                float corruption = saturate(_Intensity * _Corruption) * protect;

                // ---------- 1. Полосы со сдвигом ----------
                float bandRow = floor(uv.y * _BlockCount);
                float bandActive = step(1.0 - intensity * 0.6, Hash(float2(bandRow, tick)));
                float shift = (Hash(float2(bandRow, tick + 17.0)) - 0.5) * 2.0 * _MaxShift * intensity * bandActive;
                float2 glitchUV = uv + float2(shift, 0.0);

                // ---------- 2. Макроблоки «битого видео» ----------
                float2 grid = float2(_MacroBlocks * aspect, _MacroBlocks);
                float coarse = Hash(floor(uv * grid * 0.5) + slowTick * 0.37);
                float2 cellGrid = coarse > 0.55 ? grid * 0.5 : grid;
                float2 cell = floor(uv * cellGrid);
                float2 cellUV = frac(uv * cellGrid);

                float r0 = Hash(cell + slowTick * 1.37);
                float r1 = Hash(cell * 1.7 + slowTick + 11.0);
                float r2 = Hash(cell * 2.3 + slowTick + 23.0);

                float hit = step(r0, corruption * corruption * 1.25);

                float2 blockUV = glitchUV;
                float fromHistory = 0.0;
                float neon = 0.0;
                float drip = 0.0;

                if (hit > 0.5)
                {
                    float datamoshChance = 0.18 * _Datamosh * _HistoryValid;

                    if (r1 < 0.22)
                    {
                        // блок скопирован из другого места кадра
                        float2 jump = (float2(r2, Hash(cell + 5.0)) - 0.5) * float2(6.0, 3.0);
                        blockUV = (cell + cellUV + floor(jump)) / cellGrid;
                    }
                    else if (r1 < 0.40)
                    {
                        // пикселизация: блок «пережат» в несколько крупных пикселей
                        float q = floor(lerp(8.0, 2.0, r2));
                        blockUV = (cell + (floor(cellUV * q) + 0.5) / q) / cellGrid;
                    }
                    else if (r1 < 0.58)
                    {
                        // кровавый потёк: верхний край блока стекает вниз
                        drip = 1.0;
                        float topEdge = _SafeZoneFlip > 0.5 ? 0.02 : 0.98;
                        float wobble = (Hash(float2(cell.x + floor(cellUV.x * 6.0), slowTick)) - 0.5) * 0.04;
                        blockUV = (cell + float2(cellUV.x, topEdge - abs(wobble))) / cellGrid;
                    }
                    else if (r1 < 0.68)
                    {
                        // блок тянется вбок от своего края
                        blockUV = (cell + float2(0.02, cellUV.y)) / cellGrid;
                    }
                    else if (r1 < 0.68 + datamoshChance)
                    {
                        // датамош: блок застрял в прошлом кадре и ползёт
                        fromHistory = 1.0;
                        blockUV = uv + (float2(r2, Hash(cell + 9.0)) - 0.5) * 0.012;
                    }
                    else
                    {
                        neon = 1.0;
                    }
                }

                // ---------- Цвет: RGB расходится в розово-голубую кайму ----------
                float split = _ColorSplit * intensity * (1.0 + bandActive * 3.0 + hit * 2.0);
                half3 color;
                if (fromHistory > 0.5)
                {
                    color = SampleHistory(blockUV);
                }
                else
                {
                    color.r = SampleScreen(blockUV + float2(split, 0.0)).r;
                    color.gb = SampleScreen(blockUV - float2(split, 0.0)).gb;
                }

                // ---------- 3. Порча цвета ----------
                if (hit > 0.5)
                {
                    float bright = Brightness(color);
                    float crush = step(Hash(cell + slowTick + 41.0), _ColorCrush);
                    float r3 = Hash(cell + slowTick + 57.0);

                    half3 broken = color;
                    if (r3 < 0.35)       broken = BloodMap(bright * 1.15);            // кровь проступает
                    else if (r3 < 0.6)   broken = PaletteMap(1.0 - bright);           // розовый негатив
                    else if (r3 < 0.75)  broken = color * _AccentColor.rgb * 1.5;     // голубой призрак

                    // постеризация, как у пережатого видео
                    float levels = lerp(12.0, 3.0, _ColorCrush * corruption);
                    broken = floor(broken * levels + 0.5) / levels;

                    color = lerp(color, broken, crush);

                    // неоновые блоки: картинка перекрашивается в палитру, оттенок медленно плывёт
                    float shimmer = 0.08 * sin(_Time.y * 3.0 + r2 * 6.28);
                    color = lerp(color, PaletteMap(bright * 1.1 + shimmer), neon * saturate(0.5 + _ColorCrush * 0.5));

                    // потёк темнеет книзу, как подсыхающая кровь
                    float dripDepth = _SafeZoneFlip > 0.5 ? cellUV.y : 1.0 - cellUV.y;
                    half3 dripColor = BloodMap(Brightness(color) * 1.3) * lerp(1.0, 0.45, dripDepth);
                    color = lerp(color, dripColor, drip * 0.75);
                }

                // голубая тень сбоя в сорвавшихся полосах
                float cyanBlock = bandActive * step(0.6, Hash(float2(bandRow + floor(uv.x * 6.0) * 7.0, tick + 3.0)));
                color = lerp(color, color * _AccentColor.rgb * 1.4, cyanBlock * 0.35 * intensity);

                // ---------- 4. Строки шума в цветах палитры ----------
                float noiseRow = floor(uv.y * _ScreenParams.y * 0.5);
                float staticLine = step(1.0 - corruption * 0.04, Hash(float2(noiseRow, tick)));
                half3 noiseColor = PickNoiseColor(Hash(float2(noiseRow, tick + 2.0)))
                                 * (0.4 + 0.6 * Hash(float2(uv.x * 431.0, noiseRow + tick)));
                color = lerp(color, noiseColor, staticLine * 0.8);

                // ---------- 5. Кадр тонет в палитре ----------
                float drown = _Drown * corruption * corruption;
                color = lerp(color, PaletteMap(Brightness(color)), drown);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
