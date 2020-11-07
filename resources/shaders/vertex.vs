#version 460 core

out vec3 vColor;

uniform int time;

void main() {
    if(gl_VertexID == 0) { gl_Position = vec4(0,0,0,1); vColor = vec3(1,0,0); }
    if(gl_VertexID == 1) { gl_Position = vec4(1,0,0,1); vColor = vec3(0,1,0); }
    if(gl_VertexID == 2) { gl_Position = vec4(0,1,0,1); vColor = vec3(0,0,1); }
}
