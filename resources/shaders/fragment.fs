#version 460 core

///////////////////////////////////////////////////////////////////////////
// COMMON HEADER - move to separate file in the future
///////////////////////////////////////////////////////////////////////////

#define MAX_STEPS           50
#define MAX_DISTANCE        60.0
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

vec3 backgroundColor = vec3(0.22, 0.23, 0.35);

vec3 debugColor    = vec3(1,0,0);
bool useDebugColor = false;

///////////////////////////////////////////////////////////////////////////////
// IMPORTED FUNCTIONS
///////////////////////////////////////////////////////////////////////////////

float sdModel(vec3 position, int modelId);

///////////////////////////////////////////////////////////////////////////////
// BVH TRAVERSAL
///////////////////////////////////////////////////////////////////////////////

struct ModelIntersection {
    int model;
    float rayBegin;
    float rayEnd;
};

int modelIntersected = 0;
ModelIntersection intersectedModels[MAX_MODELS];

bool intersectBB(int nodeIndex, vec3 rayOrigin, vec3 direction, out float rayBegin, out float rayEnd) {
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
            rayEnd = tmax;
            rayBegin = tmin;
            return true;
        }
    }
    return false;
}

/**
 * inspired: https://stackoverflow.com/questions/8975773/stackless-pre-order-traversal-in-a-binary-tree
 */
bool computeModelIntersections(vec3 rayOrigin, vec3 rayDirection) {
    // traverse bvh and fidn all models
    modelIntersected = 0;
    int nodeIndex = 0;

    float rBegin;
    float rEnd;

    if (intersectBB(nodeIndex, rayOrigin, rayDirection, rBegin, rEnd)) {
        // left most search
        while (nodeIndex >= 0 && modelIntersected < MAX_MODELS) {
            BVHNode node = bvh[nodeIndex];

            if (node.model >= 0) {
                ModelIntersection intersection;
                intersection.model = node.model;
                intersection.rayBegin = rBegin;
                intersection.rayEnd = rEnd;
                intersectedModels[modelIntersected++] = intersection;
            } else {
                if (node.left > 0 && intersectBB(node.left, rayOrigin, rayDirection, rBegin, rEnd)) {
                    nodeIndex = node.left;
                    continue;
                }

                if (node.right > 0 && intersectBB(node.right, rayOrigin, rayDirection, rBegin, rEnd)) {
                    nodeIndex = node.right;
                    continue;
                }
            }

            while (node.parent >= 0) {
                BVHNode parent = bvh[node.parent];
                if (parent.left == nodeIndex && parent.right > 0) {
                    if (intersectBB(parent.right, rayOrigin, rayDirection, rBegin, rEnd)) {
                        nodeIndex = parent.right;
                        break;
                    }
                }
                nodeIndex = node.parent;
                node = parent;
            }

            if (nodeIndex == 0) {
                break;
            }
        }
    }
    return modelIntersected > 0;
}

///////////////////////////////////////////////////////////////////////////////
// RAY MARCHING
///////////////////////////////////////////////////////////////////////////////

float getHitDistance(vec3 point) {
    float d = length(point - cameraPosition);
    return clamp(d * d * HIT_DISTANCE_FACTOR, HIT_DISTANCE_MIN, HIT_DISTANCE_MAX);
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

    float closestRayBegin = 0;
    int iterations = 0;
    if (computeModelIntersections(originPoint, direction)) {
        while (iterations < modelIntersected) {

            ModelIntersection cloestMI;
            cloestMI.rayBegin = MAX_DISTANCE;
            for (int i = 0; i < modelIntersected; ++i) { // we need to find closest
                if (intersectedModels[i].rayBegin > closestRayBegin && cloestMI.rayBegin > intersectedModels[i].rayBegin) {
                    cloestMI = intersectedModels[i];
                }
            }

            float minDistance;
            vec3  actPosition = originPoint + direction * cloestMI.rayBegin;
            float intersectionDistance = cloestMI.rayEnd - cloestMI.rayBegin;
            float dist = rayMarchModel(actPosition, direction, cloestMI.model, intersectionDistance, minDistance);


            if (dist < intersectionDistance) { // hit
                modelId = cloestMI.model;
                return dist + cloestMI.rayBegin;
            }

            closestRayBegin = cloestMI.rayBegin;
            ++iterations;
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
            mat.shininess     = 300;
        } else {
            mat.color         = vec4(1.0, 0.95, 0.85, 1);
            mat.specularColor = vec4(1.1, 1.0, 0.99, 1);
            mat.shininess     = 700;
        }
    }
    return mat;
}

Material getMaterial(vec3 position, int model) {
    Material material = materials[models[model].materialId];
    if (material.textureId != INVALID_TEXTURE) {
        return  sampleProcTexture(material.textureId, position);
    }
    return material;
}

vec3 getLight(vec3 point, vec3 toLightVector, vec3 viewVector, vec3 normalVector, vec3 lightReflectedVector, int modelId) {

    float dotNL = max(dot(normalVector, toLightVector), 0.0);
    float dotRV = max(dot(lightReflectedVector, viewVector), 0.0);

    uint model;
    float dist = rayMarch(point + normalVector * getHitDistance(point) * 1.1, toLightVector, model);
    if (model != modelId && dist < length(lightPosition - point)) {
        dotNL *= 0.1;
        dotRV *= 0.1;
    }

    // get material propertios
    Material material = getMaterial(point, modelId);

    // compute light properties
    vec3 lightIntensity = vec3(0.65, 0.65, 0.65);
    vec3 ambientLight   = material.color.xyz * vec3(0.6, 0.6, 0.6);
    vec3 diffuseLight   = material.color.xyz * dotNL;
    vec3 specularLight  = material.specularColor.xyz * pow(dotRV, material.shininess);

    // combine light preperties
    return lightIntensity * (ambientLight + diffuseLight + specularLight);
}


vec3 getColor(vec3 point, int modelId, bool reflection) {
    vec3 toLightVector        = normalize(lightPosition - point);
    vec3 viewVector           = normalize(cameraPosition - point);
    vec3 normalVector         = getNormal(point, modelId);
    vec3 lightReflectedVector = normalize(reflect(-toLightVector, normalVector));
    vec3 viewReflectedVector  = normalize(reflect(-viewVector, normalVector));

    vec3 color = getLight(point, toLightVector, viewVector, normalVector, lightReflectedVector, modelId);

    if (reflection) {
        Material material = getMaterial(point, modelId);

        if (material.shininess > 100) {
            int model;
            vec3 origin = point + normalVector * getHitDistance(point) * 1.1;
            float dist = rayMarch(origin, viewReflectedVector, model);
            vec3 reflectedColor;
            if (model != modelId) {
                toLightVector        = normalize(lightPosition - origin);
                viewVector           = normalize(cameraPosition - origin);
                normalVector         = getNormal(origin, model);
                lightReflectedVector = normalize(reflect(-toLightVector, normalVector));
                origin = origin + viewReflectedVector * dist;
                reflectedColor = getLight(point, toLightVector, viewVector, normalVector, lightReflectedVector, model);
            } else {
                reflectedColor = backgroundColor;
            }
            color = mix(color, reflectedColor, material.shininess / 3000);
        }
    }

    return color;
}


///////////////////////////////////////////////////////////////////////////////
// ENTRY POINT
///////////////////////////////////////////////////////////////////////////////


void main() {
    vec3  rayOrigin    = cameraPosition;
    vec3  rayDirection = normalize(cameraDirection + fragCoord.y * upRayDistorsion + fragCoord.x * leftRayDistorsion);
    vec3  color        = backgroundColor;
    int modelId;
    float dist         = rayMarch(cameraPosition, rayDirection, modelId);

    // if hit then shade the point
    if (dist < MAX_DISTANCE) {
        // color = vec3(dist / 10);
        vec3 position = cameraPosition + rayDirection * dist;
        color = getColor(position, modelId, true);
    }

    fColor = vec4(mix(debugColor, color, useDebugColor ? 0.5 : 1), 1);
}
