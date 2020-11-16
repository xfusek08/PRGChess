#version 460 core

smooth out vec2 fragCoord;

void main() {
    int rayIndex = 0;
    switch(gl_VertexID) {
        case 0: fragCoord = vec2(-1,-1); rayIndex = 3; break; // BOTTOM LEFT
        case 1: fragCoord = vec2(-1,1);  rayIndex = 0; break; // TOP LEFT
        case 2: fragCoord = vec2(1,1);   rayIndex = 1; break; // TOP RIGHT

        case 3: fragCoord = vec2(1,1);   rayIndex = 1; break; // TOP RIGHT
        case 4: fragCoord = vec2(1,-1);  rayIndex = 2; break; // BOTTOM RIGHT
        case 5: fragCoord = vec2(-1,-1); rayIndex = 3; break; // BOTTOM LEFT
    }
    gl_Position = vec4(fragCoord,1,1);
}
