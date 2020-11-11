#version 460 core

out vec2 fragCoord;

void main() {
    switch(gl_VertexID) {
        case 0: fragCoord = vec2(-1,-1); break;
        case 1: fragCoord = vec2(-1,1);  break;
        case 2: fragCoord = vec2(1,1);   break;

        case 3: fragCoord = vec2(1,1);   break;
        case 4: fragCoord = vec2(1,-1);  break;
        case 5: fragCoord = vec2(-1,-1); break;
    }

     gl_Position = vec4(fragCoord,0,1);
}
