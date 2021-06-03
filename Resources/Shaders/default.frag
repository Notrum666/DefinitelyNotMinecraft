#version 440 core

uniform sampler2D tex;

in vec3 _v;
in vec2 _vt;
in vec3 _vn;

out vec4 outColor;

void main(void)
{
	//if (_vt.x < 0.005f || _vt.x > 0.995f || _vt.y < 0.005f || _vt.y > 0.995f)
	//	outColor = vec4(0f, 0f, 0f, 1f);
	//else
	//	outColor = texture(tex, _vt);
	outColor = texture(tex, _vt);
	if (outColor.w < 0.1)
		discard;
}