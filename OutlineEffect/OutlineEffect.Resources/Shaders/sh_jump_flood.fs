#ifndef TEXTURE_FS
#define TEXTURE_FS

#include "sh_Utils.h"

layout(location = 2) in highp vec2 v_TexCoord;

layout(std140, set = 0, binding = 0) uniform m_JumpFloodParameters
{
    int g_InitialPass;
	mediump vec2 g_Offset;
    mediump vec2 g_TexSize; 
};

layout(set = 1, binding = 0) uniform highp texture2D m_Texture;
layout(set = 1, binding = 1) uniform highp sampler m_Sampler;

layout(location = 0) out highp vec4 o_Colour;

void main(void) 
{
    vec2 closestOffset = vec2(0.0);
    float bestDist = -1.0;

    for (int u = -1; u <= 1; u++) {
        for (int v = -1; v <= 1; v++) {
            vec2 texelOffset = vec2(u, v) * g_Offset;
            vec2 texelPos = gl_FragCoord.xy + texelOffset;

            vec4 p = texelFetch(sampler2D(m_Texture, m_Sampler), ivec2(texelPos), 0);

            if (p.w > 0.5) {
                vec2 offset = (p.xy - vec2(0.5)) * 256.0 + texelOffset;

                if (g_InitialPass == 1) {
                    offset = texelPos - gl_FragCoord.xy;                    
                }        

                float dist = dot(offset, offset);

                if (bestDist < 0.0 || dist < bestDist) {
                    bestDist = dist;
                    closestOffset = offset;
                }
            }
        }
    }

    if (bestDist >= 0.0) {
        o_Colour = vec4((closestOffset / 256.0) + vec2(0.5), 0.0, 1.0);
    }
}

#endif