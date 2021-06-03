#version 440 core

layout (location = 0) in vec3 v;
layout (location = 1) in vec2 vt;
layout (location = 2) in vec3 vn;

uniform mat4 model;
uniform mat4 proj;
uniform mat4 view;

out vec3 _v;
out vec2 _vt;
out vec3 _vn;

void main(void)
{
	gl_Position = proj * view * model * vec4(v, 1.0f);
	_v = gl_Position.xyz;
	_vt = vt;
	_vn = vn;
}