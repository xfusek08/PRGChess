#version 460 core

///////////////////////////////////////////////////////////////////////////
// COMMON HEADER - move to separate file in the future
///////////////////////////////////////////////////////////////////////////

#define MAX_STEPS    50
#define MAX_DISTANCE 100.0
#define HIT_DISTANCE 0.01

#define MAX_PRIMITIVES 100
#define MAX_MODELS     10
#define MAX_MATERIALS  10

// enums

#define TYPE_SPHERE   0
#define TYPE_CAPSULE  1
#define TYPE_TORUS    2
#define TYPE_BOX      3
#define TYPE_CILINDER 4

#define OPERATION_ADD        0
#define OPERATION_SUBSTRACT  1
#define OPERATION_INTERSECT  2

#define TEXTURE_CHESSBOARD 0
#define INVALID_TEXTURE    100

struct Primitive {
    uint type;
    uint operation;
    float blending;
    // dummy float
    mat4 transform;
    vec4 data;
};

struct Material {
    vec4  color;
    vec4  specularColor;
    float shininess;
    uint  textureId; // id of procedural texture
    float textureMix;
    // dummy float
};

struct Model {
    mat4 transform;
    vec4 bbMin;
    vec4 bbMax;
    uint geometryId; // offset to primitive buffer
    uint materialId;
    uint primitiveCount;
    float scale;
};

// uniform and buffers

layout (std140) uniform PrimitivesBlock { Primitive primitives[MAX_PRIMITIVES]; };
layout (std140) uniform MaterialBlock { Material materials[MAX_MODELS]; };
layout (std140) uniform ModelsBlock { Model models[MAX_MODELS]; };

uniform uint modelsTotal;

///////////////////////////////////////////////////////////////////////////
// END OF COMMON HEADER
///////////////////////////////////////////////////////////////////////////

in vec2 fragCoord;
in vec3 rayDirection;
out vec4 fColor;

uniform vec3 cameraPosition;
uniform vec3 cameraDirection;
uniform vec3 upRayDistorsion;
uniform vec3 leftRayDistorsion;

uniform vec3  lightPosition;

vec3 debugColor     = vec3(1,0,0);
bool useDebugColor = false;


Material sampleProcTexture(uint textureId, vec3 point) {
    Material mat;
    if (textureId == TEXTURE_CHESSBOARD) {
        vec2 dim = mod(floor(point.xz), 2.0);
        if (dim.x == dim.y && all(lessThanEqual(abs(point.xz), vec2(4)))) {
            // useDebugColor = true;
            // debugColor = vec3(mod(floor(point.xz), 2.0), 0);
            mat.color         = vec4(0.1, 0.1, 0.1, 1);
            mat.specularColor = vec4(1.1, 1.0, 0.99, 1);
            mat.shininess     = 100;
        } else {
            mat.color         = vec4(1.0, 0.95, 0.85, 1);
            mat.specularColor = vec4(1.1, 1.0, 0.99, 1);
            mat.shininess     = 500;
        }
    }
    return mat;
}

// prototypes implemented in primitive_sdf
float sdToScene(vec3 position, out uint modelId);
float sdToScene(vec3 position);
float sdModel(vec3 position, uint modelId);

vec3 getNormal(vec3 point, uint modelId) {
    float d = sdModel(point, modelId);
    vec2 e = vec2(HIT_DISTANCE, 0);
    vec3 n = d - vec3(
        sdModel(point - e.xyy, modelId),
        sdModel(point - e.yxy, modelId),
        sdModel(point - e.yyx, modelId)
    );
    return normalize(n);
}

/**
 * This function returns closest bounding box which the ray intersects
 * Important properties:
 *   1. ray begins outside a BB - returned is first BB in diretion of the ray
 *      (no BB behind the ray is considered)
 *   2. ray begins inside a BB - returned is the BB in which we started such as:
 *      - bbOrigin is rayOrigin
 *      - bbEnd is where ray exits the BB
 *
 * @param out uint modelId  model id which bounding box is hitted
 * @param out vec3 bbOrigin intersection entry point to the bounding box
 * @param out vec3 bbEnd    intersection exit point to the bounding box
 *
 *  This function was inspired by: https://medium.com/@bromanz/another-view-on-the-classic-ray-aabb-intersection-algorithm-for-bvh-traversal-41125138b525
 */
bool queryModelBB(vec3 rayOrigin, vec3 rayDirection, out uint modelId, out vec3 bbOrigin, out vec3 bbEnd) {
    // find model bb intersextion
    float tmin = MAX_DISTANCE;
    float tmax = 0.0;

    for (int i = 0; i < modelsTotal; ++i) {
        vec3 ro = (models[i].transform * vec4(rayOrigin, 1)).xyz;
        vec3 inverseRayDir = 1.0 / rayDirection;

        vec3 tminv0 = (models[i].bbMin.xyz - ro) * inverseRayDir;
        vec3 tmaxv0 = (models[i].bbMax.xyz - ro) * inverseRayDir;

        vec3 tminv = min(tminv0, tmaxv0);
        vec3 tmaxv = max(tminv0, tmaxv0);

        float tminNew = max(tminv.x, max(tminv.y, tminv.z));
        float tmaxNew = min(tmaxv.x, min(tmaxv.y, tmaxv.z));

        if (tminNew < tmaxNew && tminNew < tmin && tmaxNew > 0) {
            modelId  = i;
            tmin     = max(tminNew, 0.0); // if ray starts inside, then do not reset to entery point
            tmax     = tmaxNew;
            bbOrigin = rayOrigin + rayDirection * tmin;
            bbEnd    = rayOrigin + rayDirection * tmax;
        }
    }
    return tmin < MAX_DISTANCE;
}

bool modelsIntersects(uint model1Id, uint model2Id) {
    Model model1 = models[model1Id];
    Model model2 = models[model2Id];
    bvec4 a = lessThanEqual(model1.bbMin, model2.bbMax);
    bvec4 b = greaterThanEqual(model2.bbMax, model2.bbMin);
    return all(a) && all(b);
}

float rayMarch(vec3 originPoint, vec3 direction, out uint modelId) {
    float distanceMarchedTotal = 0;
    vec3 actPosition      = originPoint;

    // act processed model
    vec3 bbOrigin;
    vec3 bbEnd;

    // get intersected bounding box and its model
    while (queryModelBB(actPosition, direction, modelId, bbOrigin, bbEnd)) {

        // debugColor = vec3(1,0,0);
        // useDebugColor = true;

        distanceMarchedTotal += length(bbOrigin - actPosition);

        // ray march inside the BB
        actPosition                = bbOrigin;
        float maxBBDistance        = length(bbEnd - bbOrigin);
        float distanceMarchedLocal = 0;
        for (int step = 0; step < MAX_STEPS; ++step) {

            // check distance to current model
            float actDist = sdModel(actPosition, modelId);
            if (actDist <= HIT_DISTANCE) {
                return distanceMarchedTotal;
            }

            // check distances to all intersected models
            uint closestModelId = modelId;
            for (int i = 0; i < modelsTotal; ++i) {
                if (i != modelId && modelsIntersects(modelId, i)) {

                    // get  distance to intersected model
                    float distToOther = sdModel(actPosition, i);

                    // return that model if hit
                    if (distToOther <= HIT_DISTANCE) {
                        modelId = i;
                        return distanceMarchedTotal;
                    }

                    // if model is closer than already found closest model - make new closest
                    if (distToOther < actDist) {
                        actDist = distToOther;
                        closestModelId = i;
                    }
                }
            }
            modelId = closestModelId;

            // make step
            actPosition          += actDist * direction;
            distanceMarchedLocal += actDist;
            distanceMarchedTotal += actDist;

            // if reached max distance - END
            if (distanceMarchedTotal >= MAX_DISTANCE) {
                return distanceMarchedTotal;
            }

            // if ray exited BB - find BB behind this one by repeating the queryModelBB
            if (distanceMarchedLocal > maxBBDistance) {
                break;
            }
        }
    }
    return MAX_DISTANCE;
}

vec3 getLight(vec3 point, uint modelId) {
    vec3 toLightVector    = normalize(lightPosition - point);
    vec3 viewVector       = normalize(cameraPosition - point);
    vec3 normalVector     = getNormal(point, modelId);
    vec3 reflectionVector = normalize(reflect(-toLightVector, normalVector));

    float dotNL = max(dot(normalVector, toLightVector), 0.0);
    float dotRV = max(dot(reflectionVector, viewVector), 0.0);

    uint shadowModel;
    float distToLight = rayMarch(point + normalVector * HIT_DISTANCE * 2, toLightVector, shadowModel);
    if (shadowModel != modelId && distToLight < length(lightPosition - point)) {
        dotNL *= 0.1;
        dotRV *= 0.1;
    }

    // get material propertios
    Material material        = materials[models[modelId].materialId];
    vec3  materialcolor      = material.color.xyz;
    vec3  specularColor      = material.specularColor.xyz;
    float shininess          = material.shininess;
    if (material.textureId != INVALID_TEXTURE) {
        Material textureMaterial = sampleProcTexture(material.textureId, point);
        materialcolor      = mix(materialcolor, textureMaterial.color.xyz,         material.textureMix);
        specularColor      = mix(specularColor, textureMaterial.specularColor.xyz, material.textureMix);
        shininess          = mix(shininess,     textureMaterial.shininess,         material.textureMix);
    }

    // compute light properties
    vec3 lightIntensity = vec3(0.65, 0.65, 0.65);
    vec3 ambientLight   = materialcolor * vec3(0.6, 0.6, 0.6);
    vec3 diffuseLight   = materialcolor * dotNL;
    vec3 specularLight  = specularColor * pow(dotRV, shininess);

    // combine light preperties
    return lightIntensity * (ambientLight + diffuseLight + specularLight);
}

void main() {
    vec3  rayOrigin    = cameraPosition;
    vec3  rayDirection = normalize(cameraDirection + fragCoord.y * upRayDistorsion + fragCoord.x * leftRayDistorsion);
    vec3  color        = vec3(0.2, 0.2, 0.3);
    uint modelId;
    float dist         = rayMarch(cameraPosition, rayDirection, modelId);

    // if hit then shade the point
    if (dist < MAX_DISTANCE) {
        // color = vec3(dist / 10);
        vec3 position = cameraPosition + rayDirection * dist;
        color = vec3(getLight(position, modelId));
    }

    fColor = vec4(mix(debugColor, color, useDebugColor ? 0.5 : 1), 1);
}
