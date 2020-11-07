#version 460 core

in vec2 position;
out vec4 fColor;

uniform uvec2 resolution;

void main(){
    vec3 cameraPosition = vec3(0, 1, 0);
    vec3 rayDirection = normalize(vec3(position.xy, 1));
    vec3 color = vec3(0,0,0);

    color = rayDirection;
    fColor = vec4(color,1);
}
