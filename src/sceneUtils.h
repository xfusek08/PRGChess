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
    glm::u32  textureId; // id of procedural texture
    glm::f32  textureMix;
    glm::f32  dummy3;
};

struct ShaderModel {
    glm::mat4 transform;
    glm::u32  geometryId;
    glm::u32  materialId;
    glm::u32  primitiveCount;
    glm::f32  scale;
};

struct ShaderBVHNode {
    glm::vec4 bbMin  = glm::vec4(0);
    glm::vec4 bbMax  = glm::vec4(0);
    glm::i32  left   = -1;
    glm::i32  right  = -1;
    glm::i32  parent = -1;
    glm::i32  model  = -1;
};

struct ShaderSceneData {
    std::vector<ShaderPrimitive> primitives;
    std::vector<ShaderModel>     models;
    std::vector<ShaderMaterial>  materials;
    std::vector<ShaderBVHNode>   bvh;
};

ShaderSceneData prepareShaderSceneData(const Scene& scene);

std::unique_ptr<Scene> buildSceneFromJson(std::string jsonFile);
