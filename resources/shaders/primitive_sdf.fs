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

#define TRANSFORM_POS(pos, obj) (obj.transform * vec4(pos, 1)).xyz

// see https://iquilezles.org/www/articles/distfunctions/distfunctions.htm

float smoothMin(float dist1, float dist2, float koeficient) {
    float h = clamp( 0.5 + 0.5 * (dist1 - dist2) / koeficient, 0.0, 1.0 );
    return mix(dist1, dist2, h ) - koeficient * h * (1.0-h);
}

float smoothMax(float dist1, float dist2, float koeficient) {
    return max(dist1, dist2);
    float h = clamp( 0.5 - 0.5 * (dist1 - dist2) / koeficient, 0.0, 1.0 );
    return mix(dist1, dist2, h ) + koeficient * h * (1.0-h);
}

// SDF definitions
float sdModel(vec3 position, uint modelId);
float sdPrimitive(vec3 position, Primitive prmiitive);
float sdSphere(vec3 position, Primitive sphere);
float sdCapsule(vec3 position, Primitive capsule);
float sdTorus(vec3 position, Primitive torus);
float sdBox(vec3 position, Primitive box);
float sdCilinder(vec3 position, Primitive cilinder);
float sdBoundingBox(vec3 position, Primitive bBox, float thicness);

float sdToScene(vec3 position, out uint modelId) { // this is for one model iside one AABB
    float finalDist = MAX_DISTANCE;
    for (int i = 0; i < modelsTotal; ++i) {
        finalDist = min(sdModel(position, i), finalDist);
        if (finalDist <= HIT_DISTANCE) {
            modelId = i;
            return finalDist;
        }
    };
    return finalDist;
};

float sdModel(vec3 position, uint modelId) {

    float finalDist = MAX_DISTANCE;
    Model model     = models[modelId];
    vec3 p          = TRANSFORM_POS(position, model);

    for (int i = 0; i < model.primitiveCount; ++i) {

        Primitive primitive = primitives[i + model.geometryId];
        primitive.blending  *= model.scale;
        float distToPrimitive = sdPrimitive(p / model.scale, primitive) * model.scale;

        switch (primitive.operation) {
            case OPERATION_ADD:       finalDist = smoothMin(distToPrimitive, finalDist, primitive.blending); break;
            case OPERATION_SUBSTRACT: finalDist = smoothMax(-distToPrimitive, finalDist, primitive.blending); break;
            case OPERATION_INTERSECT: finalDist = smoothMax(distToPrimitive, finalDist, primitive.blending); break;
        };
    }

    // optimize with dist to models bounding box

    // data.y *= 0.5;
    // model.bbMax *= model.scale;
    // model.bbMin *= model.scale;

    Primitive bb = {
        TYPE_BOX,
        OPERATION_ADD,
        0,
        model.transform,
        (model.bbMax - model.bbMin) * 0.5
    };
    finalDist = min(sdBoundingBox(
        (position - (model.bbMin.xyz * 0.5 + model.bbMax.xyz * 0.5)),
        bb,
        0.01
    ), finalDist);
    // finalDist = min(sdBox(position, bb), finalDist);


    return finalDist;
}

float sdToScene(vec3 position) {
    uint dummy = 0;
    return sdToScene(position, dummy);
}

float sdPrimitive(vec3 position, Primitive prmiitive) {
    switch (prmiitive.type) {
        case TYPE_SPHERE:   return sdSphere(position, prmiitive);
        case TYPE_CAPSULE:  return sdCapsule(position, prmiitive);
        case TYPE_TORUS:    return sdTorus(position, prmiitive);
        case TYPE_BOX:      return sdBox(position, prmiitive);
        case TYPE_CILINDER: return sdCilinder(position, prmiitive);
    }
    return MAX_DISTANCE;
}

// primitive SD functions

float sdSphere(vec3 position, Primitive sphere) {
    return length(TRANSFORM_POS(position, sphere)) - sphere.data.x;
}

float sdCapsule(vec3 position, Primitive capsule) {
    vec3 p = TRANSFORM_POS(position, capsule);
    vec3 a = vec3(0,  0.5, 0) * capsule.data.y;
    vec3 b = vec3(0, -0.5, 0) * capsule.data.y;
    vec3 ab = b - a;
    vec3 ap = p - a;
    float t = clamp(dot(ab, ap) / dot(ab, ab), 0, 1);
    return length(ap - ab * t) - capsule.data.x;
}

float sdTorus(vec3 position, Primitive torus) {
    vec3 p = TRANSFORM_POS(position, torus);
    float x = length(p.xz) - torus.data.x;
    return length(vec2(x, p.y)) - torus.data.y;
}

float sdBox(vec3 position, Primitive box) {
    vec3  p = TRANSFORM_POS(position, box);
    vec3  d = abs(p) - box.data.xyz + box.data.www;
    float e = length(max(d, 0.0));               // exterior distance
    float i = min(max(d.x, max(d.y, d.z)), 0.0); // interior distance
    return e + i - box.data.w;
}

float sdCilinder(vec3 position, Primitive cilinder) {
    vec3 p = TRANSFORM_POS(position, cilinder);
    float w = cilinder.data.x;
    float h = cilinder.data.y;
    vec2  d = abs(vec2(length(p.xz), p.y)) - vec2(w, h);
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
}

float sdBoundingBox(vec3 position, Primitive bBox, float thicness)
{
    vec3 p = TRANSFORM_POS(position, bBox);
    p = abs(p) - bBox.data.xyz;
    vec3 q = abs(p + thicness) - thicness;
    return min(
        min(
            length(max(vec3(p.x,q.y,q.z),0.0)) + min(max(p.x,max(q.y,q.z)),0.0),
            length(max(vec3(q.x,p.y,q.z),0.0)) + min(max(q.x,max(p.y,q.z)),0.0)
        ),
        length(max(vec3(q.x,q.y,p.z),0.0)) + min(max(q.x,max(q.y,p.z)),0.0)
    );
}
