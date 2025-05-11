#ifndef POSITION_FS
#define POSITION_FS

#include "sh_Utils.h"

layout(location = 2) in highp vec2 v_TexCoord;
layout(location = 1) in lowp vec4 v_Colour;

layout(std140, set = 0, binding = 0) uniform m_OutlineParameters
{
    mediump vec2 g_TexSize;    
    mediump float g_OutlineWidth;
};

layout(set = 1, binding = 0) uniform highp texture2D m_Texture;
layout(set = 1, binding = 1) uniform highp sampler m_Sampler;

layout(location = 0) out highp vec4 o_Colour;

void main(void) 
{
    vec4 p = texelFetch(sampler2D(m_Texture, m_Sampler), ivec2(v_TexCoord * g_TexSize), 0);

    if (p.w > 0.0) {
        vec2 position = (p.xy - 0.5) * 256.0;
        float dist = length(position) - g_OutlineWidth;

        float alpha = clamp(1.0 - dist, 0.0, 1.0);

        o_Colour = v_Colour * alpha;
    }
}

#endif