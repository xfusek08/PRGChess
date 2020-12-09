
#include <unordered_map>
#include <fstream>

#include <sceneUtils.h>
#include <AABB.h>
#include <nlohmann/json.hpp>
#include <glm/gtc/type_ptr.hpp>
#include <RenderBase/tools/logging.h>

#define EXPAND(x) x
#define GET_MACRO_3(_1, _2, _3, NAME, ...) NAME
#define GET_MACRO_4(_1, _2, _3, _4, NAME, ...) NAME

#define IF_SET(propName)                                                   if (value.contains(#propName))
#define _4_SET_PROPERTY(type, target, propName, targetProperty, operation) IF_SET(propName) { target.targetProperty = value[#propName].get<type>(); operation }
#define _3_SET_PROPERTY(type, target, propName, targetProperty)            _4_SET_PROPERTY(type, target, propName, targetProperty, )
#define _2_SET_PROPERTY(type, target, propName)                            _3_SET_PROPERTY(type, target, propName, propName)
#define SET_PROPERTY(type, ...)                                            EXPAND(GET_MACRO_4(__VA_ARGS__, _4_SET_PROPERTY, _3_SET_PROPERTY, _2_SET_PROPERTY, invalid) (type, __VA_ARGS__ ))

#define SET_PROPERTY_FLOAT(...)                           SET_PROPERTY(float, __VA_ARGS__)
#define SET_PROPERTY_INT(...)                             SET_PROPERTY(int, __VA_ARGS__)
#define SET_PROPERTY_STRING(...)                          SET_PROPERTY(string, __VA_ARGS__)

#define SET_PROPERTY_VEC(target, propName, vecDim)        IF_SET(propName) { target.propName = jsonToVec<vecDim>(value[#propName]); }
#define SET_PROPERTY_VEC3(target, propName)               SET_PROPERTY_VEC(target, propName, 3)
#define SET_PROPERTY_VEC4(target, propName)               SET_PROPERTY_VEC(target, propName, 4)

#define _2_SET_PROPERTY_TRANSFORM(target, targetProperty) { target.targetProperty = jsonToTransform(value); }
#define _1_SET_PROPERTY_TRANSFORM(target)                 _2_SET_PROPERTY_TRANSFORM(target, transform)
#define SET_PROPERTY_TRANSFORM(...)                       EXPAND(GET_MACRO_3(offset, __VA_ARGS__, _2_SET_PROPERTY_TRANSFORM, _1_SET_PROPERTY_TRANSFORM) ( __VA_ARGS__ ))

using namespace std;
using Json = nlohmann::json;

template<size_t L>
glm::vec<L, float> jsonToVec(Json value) {
    auto res = glm::vec<L, float>(0);
    int index = 0;
    for (auto& val : value) {
        res[index++] = val.get<float>();
        if (index >= L) break;
    }
    return res;
}

Transform jsonToTransform(Json value) {
    auto transform = Transform();
    SET_PROPERTY_VEC3(transform, position)
    SET_PROPERTY_VEC3(transform, rotation)
    SET_PROPERTY_FLOAT(transform, size)
    return transform;
}

unique_ptr<Scene> buildSceneFromJson(string jsonFile) {

    // load json
    Json json;
    std::ifstream stream(jsonFile);
    if (stream.good()) {
        stream >> json;
    } else {
        json = Json::parse(jsonFile);
    }

    auto scene = make_unique<Scene>();

    // fill materials
    for (auto& [key, value] : json["materials"].items()) {
        auto material = Material();
        SET_PROPERTY_VEC3(material, color)
        SET_PROPERTY_VEC3(material, specularColor)
        SET_PROPERTY_FLOAT(material, shininess)
        SET_PROPERTY(TextureType, material, textureType)
        SET_PROPERTY_FLOAT(material, textureMix)
        scene->materials[key] = material;
    }

    // fill geometries
    for (auto& [key, primitives] : json["geometries"].items()) {
        auto geometry = ModelGeometry();

        // per primitive
        for (auto& value : primitives) {
            IF_SET(type) {
                auto primitive = Primitive();

                // common properties
                IF_SET(type)      primitive.type      = Primitive::typeFromString(value["type"].get<string>());
                IF_SET(operation) primitive.operation = Primitive::operationFromString(value["operation"].get<string>());
                SET_PROPERTY_TRANSFORM(primitive)
                SET_PROPERTY_FLOAT(primitive, blending)
                SET_PROPERTY_VEC4(primitive, data)

                // type specifics
                switch (primitive.type)  {
                    case PrimitiveType::Sphere:
                        primitive.data = { 0.5f, 0.0f, 0.0f, 0.0f };

                        SET_PROPERTY_FLOAT(primitive, diameter, data.x, primitive.data.x *= 0.5f; )
                        SET_PROPERTY_FLOAT(primitive, radius,   data.x)
                        break;
                    case PrimitiveType::Box:
                        primitive.data = { 0.5f, 0.5f, 0.5f, 0.0f };

                        SET_PROPERTY_FLOAT(primitive, width,  data.x, primitive.data.x *= 0.5f;)
                        SET_PROPERTY_FLOAT(primitive, height, data.y, primitive.data.y *= 0.5f;)
                        SET_PROPERTY_FLOAT(primitive, depth,  data.z, primitive.data.z *= 0.5f;)
                        SET_PROPERTY_FLOAT(primitive, radius, data.w)
                        break;
                    case PrimitiveType::Cilinder:
                        primitive.data = { 1.0f, 1.0f, 0.0f, 0.0f };

                        SET_PROPERTY_FLOAT(primitive, width,   data.x)
                        SET_PROPERTY_FLOAT(primitive, height,  data.y, primitive.data.y *= 0.5f;)
                        SET_PROPERTY_FLOAT(primitive, radius,  data.x)
                        SET_PROPERTY_FLOAT(primitive, rounded, data.z)
                        break;

                    case PrimitiveType::Capsule:
                        primitive.data = { 1.0f, 1.0f, 0.0f, 0.0f };

                        SET_PROPERTY_FLOAT(primitive, width,  data.x, primitive.data.y *= 0.5f;)
                        SET_PROPERTY_FLOAT(primitive, height, data.y, primitive.data.y *= 0.5f;)
                        SET_PROPERTY_FLOAT(primitive, radius, data.x)
                        break;

                    case PrimitiveType::Torus:
                        primitive.data = { 1.0f, 1.0f, 0.0f, 0.0f };

                        SET_PROPERTY_FLOAT(primitive, diameterX, data.x, primitive.data.x *= 0.5f;)
                        SET_PROPERTY_FLOAT(primitive, diameterY, data.y, primitive.data.y *= 0.5f;)
                        SET_PROPERTY_FLOAT(primitive, radiusX,   data.x)
                        SET_PROPERTY_FLOAT(primitive, radiusY,   data.y)
                        break;

                    case PrimitiveType::Cone:
                        primitive.data = { 0.5f, 0.25f, 0.5f, 0.0f };

                        SET_PROPERTY_FLOAT(primitive, height,         data.z, primitive.data.z *= 0.5f;)
                        SET_PROPERTY_FLOAT(primitive, rounded,        data.w)

                        SET_PROPERTY_FLOAT(primitive, diameterBottom, data.x, primitive.data.x *= 0.5f;)
                        SET_PROPERTY_FLOAT(primitive, diameterTop,    data.y, primitive.data.y *= 0.5f;)

                        SET_PROPERTY_FLOAT(primitive, radiusBottom,   data.x)
                        SET_PROPERTY_FLOAT(primitive, radiusTop,      data.y)
                        break;

                    case PrimitiveType::RoundCone:
                        primitive.data = { 0.5f, 0.25f, 0.5f, 0.0f };

                        SET_PROPERTY_FLOAT(primitive, height,         data.z, primitive.data.z *= 0.5f;)

                        SET_PROPERTY_FLOAT(primitive, diameterBottom, data.x, primitive.data.x *= 0.5f;)
                        SET_PROPERTY_FLOAT(primitive, diameterTop,    data.y, primitive.data.y *= 0.5f;)

                        SET_PROPERTY_FLOAT(primitive, radiusBottom,   data.x)
                        SET_PROPERTY_FLOAT(primitive, radiusTop,      data.y)
                        break;

                    default:
                        LOG_DEBUG("Invalid primitive type.");
                        continue;
                }

                geometry.primitives.push_back(primitive);
            }
        }

        scene->geometries[key] = geometry;
    }

    // fill models
    scene->models.reserve(json["models"].size());
    for (auto& [key, value] : json["models"].items()) {
        auto model = Model();
        SET_PROPERTY_TRANSFORM(model)
        SET_PROPERTY_STRING(model, geometry, geometryIdent)
        SET_PROPERTY_STRING(model, material, materialIdent)
        assert(!model.geometryIdent.empty() && !model.materialIdent.empty());
        scene->models.push_back(model);
    }

    return scene;
}

ShaderSceneData prepareShaderSceneData(const Scene& scene) {
    auto data = ShaderSceneData();

    // model geometry identification map - geometry_ident -> (primitive_offset, primitive_count)
    unordered_map<string, tuple<uint32_t, uint32_t>> mgIdentMap;

    // material identification map - material_ident -> material_offset
    unordered_map<string, uint32_t> maIdentMap;

    // load primitives to data and fill mgIdentMap
    uint32_t actId = 0;
    for (const auto& actGeometry : scene.geometries) {
        uint32_t count = 0;
        for (const auto& actPrimitive : actGeometry.second.primitives) {
            ++count;
            auto shaderPrimitive      = ShaderPrimitive();
            shaderPrimitive.type      = actPrimitive.type;
            shaderPrimitive.transform = actPrimitive.transform.getTransform();
            shaderPrimitive.data      = actPrimitive.data;
            shaderPrimitive.operation = actPrimitive.operation;
            shaderPrimitive.blending  = actPrimitive.blending;
            data.primitives.push_back(shaderPrimitive);
        }
        mgIdentMap[actGeometry.first] = { actId, count };
        actId += count;
    }

    // load materials to data and fill maIdentMap
    actId = 0;
    for (const auto& actMaterial : scene.materials) {
        auto shaderMaterial          = ShaderMaterial();
        shaderMaterial.color         = glm::vec4(actMaterial.second.color, 1.0);
        shaderMaterial.specularColor = glm::vec4(actMaterial.second.specularColor, 1.0);
        shaderMaterial.shininess     = actMaterial.second.shininess;;
        shaderMaterial.textureId     = actMaterial.second.textureType;
        shaderMaterial.textureMix    = actMaterial.second.textureMix;
        data.materials.push_back(shaderMaterial);

        maIdentMap[actMaterial.first] = actId;
        ++actId;
    }

    // load models to data
    for (const auto& actModel : scene.models) {
        auto aabb = AABBHierarchy(scene);
        auto bb = aabb.geometryBB(actModel.geometryIdent);
        auto shaderModel           = ShaderModel();
        shaderModel.transform      = actModel.transform.getTransform();
        shaderModel.bbMin          = glm::vec4(bb.min, 1.0) * actModel.transform.size;
        shaderModel.bbMax          = glm::vec4(bb.max, 1.0) * actModel.transform.size;
        shaderModel.geometryId     = get<0>(mgIdentMap[actModel.geometryIdent]);
        shaderModel.primitiveCount = get<1>(mgIdentMap[actModel.geometryIdent]);
        shaderModel.materialId     = maIdentMap[actModel.materialIdent];
        shaderModel.scale          = actModel.transform.size;
        data.models.push_back(shaderModel);
    }

    return data;
}
