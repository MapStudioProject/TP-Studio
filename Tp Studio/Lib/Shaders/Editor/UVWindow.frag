#version 330

uniform sampler2D uvTexture;

in vec2 TexCoords;

uniform float brightness;
uniform int isSRGB;
uniform int hasTexture;
uniform int isBC5S;
uniform int mipLevel;

uniform vec4 uColor;

out vec4 fragOutput;

void main()
{  
    vec4 color = uColor;
    if (hasTexture == 1)
    {
        color *= textureLod(uvTexture, TexCoords, mipLevel);
        if (isBC5S == 1) //BC5 Snorm conversion
        {
           color.rg = (color.rg + 1.0) / 2.0;
           color.b = color.b;
        }
    }
    fragOutput = vec4(color.rgb, 1.0);

    if (isSRGB == 1)
        fragOutput.rgb = pow(fragOutput.rgb, vec3(1.0/2.2));

    fragOutput.rgb *= brightness;
}  