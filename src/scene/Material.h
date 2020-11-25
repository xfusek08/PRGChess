#pragma once

#include <scene/Transform.h>

enum TextureType {
    chessboard = 0,
    ttInvalid  = 100,
};

class Material
{
    public:
        glm::vec3   color         = glm::vec3(1.0);
        glm::vec3   specularColor = glm::vec3(0.0f);
        glm::f32    shininess     = 1;
        TextureType textureType   = TextureType::ttInvalid; // id of procedural texture
        glm::f32    textureMix    = 0.0;
};
