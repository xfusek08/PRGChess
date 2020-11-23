#pragma once

#include <scene/Transform.h>

class Material
{
    public:
        glm::vec3 color         = glm::vec3(1.0);
        glm::vec3 specularColor = glm::vec3(1.0f);
        glm::f32  shininess     = 10;
        // texture in future
};
