#version 460 core

out vec2 position;

void main() {
    switch(gl_VertexID) {
        case 0: position = vec2(-1,-1); break;
        case 1: position = vec2(-1,1);  break;
        case 2: position = vec2(1,1);   break;

        case 3: position = vec2(1,1);   break;
        case 4: position = vec2(1,-1);  break;
        case 5: position = vec2(-1,-1); break;
    }

     gl_Position = vec4(position,0,1);
}
