
#include <sceneGpuUtils.h>

#include <AABB.h>

#include <unordered_map>

using namespace std;

unique_ptr<ShaderSceneData> prepareShaderSceneData(const Scene& scene) {
    auto data = make_unique<ShaderSceneData>();

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
            data->primitives.push_back(shaderPrimitive);
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
        data->materials.push_back(shaderMaterial);

        maIdentMap[actMaterial.first] = actId;
        ++actId;
    }

    // load models to data
    for (const auto& actModel : scene.models) {
        auto aabb = AABBHierarchy(scene);
        auto bb = aabb.geometryBB(actModel.geometryIdent);
        auto shaderModel           = ShaderModel();
        shaderModel.transform      = actModel.transform.getTransform();
        shaderModel.bbMin          = glm::vec4(bb.min, 1.0);
        shaderModel.bbMax          = glm::vec4(bb.max, 1.0);
        shaderModel.geometryId     = get<0>(mgIdentMap[actModel.geometryIdent]);
        shaderModel.primitiveCount = get<1>(mgIdentMap[actModel.geometryIdent]);
        shaderModel.materialId     = maIdentMap[actModel.materialIdent];
        data->models.push_back(shaderModel);
    }

    return data;
}
