#pragma once

#include <scene/Scene.h>

#include <memory>

struct ShaderPrimitive {
    glm::u32  type;
    glm::u32  operation;
    glm::f32  blending;
    glm::f32  dummy;
    glm::mat4 transform;
    glm::vec4 data;
};

struct ShaderMaterial {
    glm::vec4 color;
    glm::vec4 specularColor;
    glm::f32  shininess;
    glm::f32  dummy1;
    glm::f32  dummy2;
    glm::f32  dummy3;
};

struct ShaderModel {
    glm::mat4 transform;
    glm::vec4 bbMin;
    glm::vec4 bbMax;
    glm::u32  geometryId;
    glm::u32  materialId;
    glm::u32  primitiveCount;
    glm::f32  scale;
};

struct ShaderSceneData {
    std::vector<ShaderPrimitive> primitives;
    std::vector<ShaderModel>     models;
    std::vector<ShaderMaterial>  materials;
};

std::unique_ptr<ShaderSceneData> prepareShaderSceneData(const Scene& scene);
