#version 460 core

///////////////////////////////////////////////////////////////////////////
// COMMON HEADER - move to separate file in the future
///////////////////////////////////////////////////////////////////////////

#define MAX_STEPS    100
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
    // dummy float
    // dummy float
    // dummy float
};

struct Model {
    mat4 transform;
    vec4 bbMin;
    vec4 bbMax;
    uint geometryId; // offset to primitive buffer
    uint materialId;
    uint primitiveCount;
    // float dummy
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

// act global processed state
Model actModel;
vec3 modelBBOrigin;
vec3 modelBBEnd;

// prototypes implemented in primitive_sdf
float sdToScene(vec3 position, out uint modelId);
float sdToScene(vec3 position);

vec3 getNormal(vec3 point) {
    float d = sdToScene(point);
    vec2 e = vec2(HIT_DISTANCE, 0);
    vec3 n = d - vec3(
        sdToScene(point - e.xyy),
        sdToScene(point - e.yxy),
        sdToScene(point - e.yyx)
    );
    return normalize(n);
}

float rayMarch(vec3 originPoint, vec3 direction, out uint modelId) {
    float distanceMarched = 0;
    vec3 atcPosition = originPoint;
    for (int step = 0; step < MAX_STEPS; ++step) {
        float actDist = sdToScene(atcPosition, modelId);
        atcPosition += actDist * direction;
        distanceMarched += actDist;
        if (distanceMarched > MAX_DISTANCE || distanceMarched < HIT_DISTANCE) {
            break; // hit
        }
    }
    return distanceMarched;
}

vec3 getLight(vec3 point, uint modelId) {
    vec3 toLightVector    = normalize(lightPosition - point);
    vec3 viewVector       = normalize(cameraPosition - point);
    vec3 normalVector     = getNormal(point);
    vec3 reflectionVector = normalize(reflect(-toLightVector, normalVector));

    float dotNL;
    float dotRV;

    uint shadowModel;
    float distToLight = rayMarch(point + normalVector * HIT_DISTANCE * 2, toLightVector, shadowModel);
    if (shadowModel != modelId && distToLight < length(lightPosition - point)) {
        dotNL = 0;
        dotRV = 0;
    } else {
        dotNL = max(dot(normalVector, toLightVector), 0.0);
        dotRV = max(dot(reflectionVector, viewVector), 0.0);
    }

    // material properties
    Material material   = materials[models[modelId].materialId];
    vec3  materialcolor = material.color.xyz;
    vec3  specularColor = material.specularColor.xyz;
    float shininess     = material.shininess;

    // light properties
    vec3 lightIntensity = vec3(0.6, 0.6, 0.6);
    vec3 ambientLight   = materialcolor * vec3(0.3, 0.3, 0.3);
    vec3 diffuseLight   = materialcolor * dotNL;
    vec3 specularLight  = specularColor * pow(dotRV, shininess);

    return lightIntensity * (ambientLight + diffuseLight + specularLight);
}


bool queryModelBB(vec3 rayOrigin, vec3 rayDirection) {
    // find model bb intersextion
    float tmin = 0.0;
    float tmax = MAX_DISTANCE;

    for (int i = 0; i < modelsTotal; ++i) {
        vec3 inverseRayDir = 1.0 / rayDirection;

        vec3 tminv = (models[i].bbMin.xyz - rayOrigin) * inverseRayDir;
        vec3 tmaxv = (models[i].bbMax.xyz - rayOrigin) * inverseRayDir;
        tminv = min(tminv, tmaxv);
        tmaxv = max(tminv, tmaxv);

        float tminNew = max(tminv.x, max(tminv.y, tminv.z));
        float tmaxNew = min(tmaxv.x, max(tmaxv.y, tmaxv.z));

        if (tminNew < tmaxNew && tminNew < tmin) {
            tmin          = tminNew;
            tmax          = tmaxNew;
            actModel      = models[i];
            modelBBOrigin = rayOrigin + rayDirection * tmin;
            modelBBEnd    = rayOrigin + rayDirection * tmax;
        }
    }
    return tmin > 0.0;
}

void main() {
    // vec3 rayOrigin    = cameraDirection;
    // vec3 rayDirection = normalize(rayOrigin + fragCoord.y * upRayDistorsion + fragCoord.x * leftRayDistorsion);
    // vec3 color        = vec3(0.2, 0.2, 0.3);

    // while (queryModelBB(rayOrigin, rayDirection) {

    //     // raymarch inside bb volume
    //     float intersectionLenght = length(modelBBEnd - modelBBOrigin);
    //     float dist = rayMarch(rayOrigin, rayDirection, intersectionLenght);
    //     vec3 position = modelBBOrigin + rayDirection * dist;

    //     if (dist < intersectionLenght) { // hit
    //         color = vec3(getLight(position, modelId));
    //         break;
    //     } else {
    //         rayOrigin = position;
    //     }
    // }

    vec3  rayDirection = normalize(cameraDirection + fragCoord.y * upRayDistorsion + fragCoord.x * leftRayDistorsion);
    uint model;
    float dist         = rayMarch(cameraPosition, rayDirection, model);
    vec3  color        = vec3(0.2, 0.2, 0.3);

    if (dist < MAX_DISTANCE) {
        vec3 position = cameraPosition + rayDirection * dist;
        color         = vec3(getLight(position, model));
    }

    fColor = vec4(color, 1);
}
