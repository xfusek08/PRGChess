#version 460 core

///////////////////////////////////////////////////////////////////////////
// COMMON HEADER - move to separate file in the future
///////////////////////////////////////////////////////////////////////////

#define MAX_STEPS           30
#define MAX_DISTANCE        100.0
#define HIT_DISTANCE_MAX    0.5
#define HIT_DISTANCE_MIN    0.003
#define HIT_DISTANCE_FACTOR 0.0001

#define MAX_BVH_SIZE   100 // given the formula of 2*l - 1
#define MAX_PRIMITIVES 100
#define MAX_MODELS     50
#define MAX_MATERIALS  10

// enums

#define TYPE_SPHERE     0
#define TYPE_CAPSULE    1
#define TYPE_TORUS      2
#define TYPE_BOX        3
#define TYPE_CILINDER   4
#define TYPE_CONE       5
#define TYPE_ROUND_CONE 6

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
    uint geometryId; // offset to primitive buffer
    uint materialId;
    uint primitiveCount;
    float scale;
};

struct BVHNode {
    vec4 bbMin;
    vec4 bbMax;
    int left;
    int right;
    int parent;
    int model;
};

// uniform and buffers

layout (std140) uniform PrimitivesBlock { Primitive primitives[MAX_PRIMITIVES]; };
layout (std140) uniform MaterialBlock { Material materials[MAX_MODELS]; };
layout (std140) uniform ModelsBlock { Model models[MAX_MODELS]; };
layout (std140) uniform BVHBlock { BVHNode bvh[MAX_BVH_SIZE]; };

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

vec3 debugColor    = vec3(1,0,0);
bool useDebugColor = false;

///////////////////////////////////////////////////////////////////////////////
// IMPORTED FUNCTIONS
///////////////////////////////////////////////////////////////////////////////

float sdModel(vec3 position, int modelId);

///////////////////////////////////////////////////////////////////////////////
// RAY MARCHING
///////////////////////////////////////////////////////////////////////////////

float getHitDistance(vec3 point) {
    float d = length(point - cameraPosition);
    return clamp(d * d * HIT_DISTANCE_FACTOR, HIT_DISTANCE_MIN, HIT_DISTANCE_MAX);
}

float intersectBB(int nodeIndex, vec3 rayOrigin, vec3 direction, out float toEnd) {
    if (nodeIndex >= 0) {
        vec3 ro = (vec4(rayOrigin, 1)).xyz;
        vec3 inverseRayDir = 1.0 / direction;

        vec3 tminv0 = (bvh[nodeIndex].bbMin.xyz - ro) * inverseRayDir;
        vec3 tmaxv0 = (bvh[nodeIndex].bbMax.xyz - ro) * inverseRayDir;

        vec3 tminv = min(tminv0, tmaxv0);
        vec3 tmaxv = max(tminv0, tmaxv0);

        float tmin = max(tminv.x, max(tminv.y, tminv.z));
        float tmax = min(tmaxv.x, min(tmaxv.y, tmaxv.z));

        if (tmin < tmax && tmax > 0) {
            toEnd = tmax;
            return tmin;
        }
    }
    return MAX_DISTANCE;
}

int closestIntersectedChild(int nodeIndex, vec3 rayOrigin, vec3 direction, out float toBegin, out float toEnd) {
    float parentBegin = intersectBB(nodeIndex, rayOrigin, direction, toEnd);
    if (parentBegin >= MAX_DISTANCE) {
        return -1;
    } else if (bvh[nodeIndex].model >= 0) {
        toBegin = parentBegin;
        return nodeIndex;
    }

    BVHNode parent = bvh[nodeIndex];

    float lEnd, rEnd;
    float lBegin = intersectBB(parent.left, rayOrigin, direction, lEnd);
    float rBegin = intersectBB(parent.right, rayOrigin, direction, rEnd);
    bool lValid = lBegin < MAX_DISTANCE;
    bool rValid = rBegin < MAX_DISTANCE;

    if (!lValid && !rValid) {
        toBegin = toEnd + getHitDistance(rayOrigin) * 1.1;
        toEnd = MAX_DISTANCE;
        return 0;
    }

    if (abs(lBegin) <= abs(rBegin)) {
        toEnd = lEnd;
        toBegin = lBegin;
        return parent.left;
    }

    toEnd = rEnd;
    toBegin = rBegin;
    return parent.right;
}

float rayMarchModel(vec3 originPoint, vec3 direction, int modelId, float maxDistance, out float minDistance) {
    float distanceMarched = 0;
    minDistance = maxDistance;
    for (int step = 0; step < MAX_STEPS; ++step) {
        vec3 position = originPoint + distanceMarched * direction;
        float dist = sdModel(position, modelId);
        minDistance = min(dist, minDistance);
        distanceMarched += dist;
        if (dist <= getHitDistance(position) || distanceMarched >= maxDistance) {
            break;
        }
    }
    return min(distanceMarched, maxDistance);
}

float rayMarch(vec3 originPoint, vec3 direction, out int modelId) {
    float distanceMarchedTotal = 0;
    vec3  actPosition = originPoint;
    int   nodeIndex = 0;
    float toBegin = MAX_DISTANCE;
    float toEnd = MAX_DISTANCE;

    // useDebugColor = true;
    // debugColor = vec3(0);

    while ((nodeIndex = closestIntersectedChild(nodeIndex, actPosition, direction, toBegin, toEnd)) >= 0) {
        // debugColor += vec3(0.2, 0, 0);
        if (toEnd >= MAX_DISTANCE) {
            distanceMarchedTotal += toBegin;
            actPosition += direction * toBegin;
            nodeIndex = 0;
        } else
        if (bvh[nodeIndex].model >= 0) {
            BVHNode node = bvh[nodeIndex];

            distanceMarchedTotal += toBegin;
            actPosition += direction * toBegin;
            float intersectionDistance = toEnd - toBegin;

            float minDistance = MAX_DISTANCE; // dummy for now
            float dist = rayMarchModel(actPosition, direction, node.model, intersectionDistance, minDistance);

            distanceMarchedTotal += dist;
            if (dist < intersectionDistance) { // this is hit
                modelId = node.model;
                return distanceMarchedTotal;
            } else {
                // move act position behind bb and query tree again
                actPosition += direction * (dist + getHitDistance(actPosition));
                nodeIndex = 0;
            }
        }
    }
    return MAX_DISTANCE;



}

///////////////////////////////////////////////////////////////////////////////
// MATERIALS AND LIGTHING
///////////////////////////////////////////////////////////////////////////////

vec3 getNormal(vec3 point, int modelId) {
    float d = sdModel(point, modelId);
    vec2 e = vec2(getHitDistance(point), 0);
    vec3 n = d - vec3(
        sdModel(point - e.xyy, modelId),
        sdModel(point - e.yxy, modelId),
        sdModel(point - e.yyx, modelId)
    );
    return normalize(n);
}

Material sampleProcTexture(uint textureId, vec3 point) {
    Material mat;
    if (textureId == TEXTURE_CHESSBOARD) {
        vec2 dim = mod(floor(point.xz), 2.0);
        if (dim.x == dim.y && all(lessThanEqual(abs(point.xz), vec2(4)))) {
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

vec3 getLight(vec3 point, int modelId) {
    vec3 toLightVector    = normalize(lightPosition - point);
    vec3 viewVector       = normalize(cameraPosition - point);
    vec3 normalVector     = getNormal(point, modelId);
    vec3 reflectionVector = normalize(reflect(-toLightVector, normalVector));

    float dotNL = max(dot(normalVector, toLightVector), 0.0);
    float dotRV = max(dot(reflectionVector, viewVector), 0.0);

    // uint shadowModel;
    // float distToLight = rayMarch(point + normalVector * getHitDistance(point) * 2, toLightVector, shadowModel);
    // if (shadowModel != modelId && distToLight < length(lightPosition - point)) {
    //     dotNL *= 0.1;
    //     dotRV *= 0.1;
    // }

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


///////////////////////////////////////////////////////////////////////////////
// ENTRY POINT
///////////////////////////////////////////////////////////////////////////////


void main() {
    vec3  rayOrigin    = cameraPosition;
    vec3  rayDirection = normalize(cameraDirection + fragCoord.y * upRayDistorsion + fragCoord.x * leftRayDistorsion);
    vec3  color        = vec3(0.2, 0.2, 0.3);
    int modelId;
    float dist         = rayMarch(cameraPosition, rayDirection, modelId);

    // if hit then shade the point
    if (dist < MAX_DISTANCE) {
        // color = vec3(dist / 10);
        vec3 position = cameraPosition + rayDirection * dist;
        color = vec3(getLight(position, modelId));
    }

    fColor = vec4(mix(debugColor, color, useDebugColor ? 0.5 : 1), 1);
}
