#version 460 core

///////////////////////////////////////////////////////////////////////////
// COMMON HEADER - move to separate file in the future
///////////////////////////////////////////////////////////////////////////

#define MAX_STEPS           25
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

#define TRANSFORM_POS(pos, obj) (obj.transform * vec4(pos, 1)).xyz

// see https://iquilezles.org/www/articles/distfunctions/distfunctions.htm

float smoothMin(float dist1, float dist2, float koeficient) {
    float h = clamp( 0.5 + 0.5 * (dist1 - dist2) / koeficient, 0.0, 1.0 );
    return mix(dist1, dist2, h ) - koeficient * h * (1.0-h);
}

float smoothMax(float dist1, float dist2, float koeficient) {
    float h = clamp( 0.5 - 0.5 * (dist1 - dist2) / koeficient, 0.0, 1.0 );
    return mix(dist1, dist2, h ) + koeficient * h * (1.0-h);
}

float getHitDistance(vec3 point);

// SDF definitions
float sdModel(vec3 position, int modelId);
float sdPrimitive(vec3 position, Primitive primitive);
float sdSphere(vec3 position, Primitive sphere);
float sdCapsule(vec3 position, Primitive capsule);
float sdTorus(vec3 position, Primitive torus);
float sdBox(vec3 position, Primitive box);
float sdCilinder(vec3 position, Primitive cilinder);
float sdCone(vec3 position, Primitive cone);
float roundCone(vec3 position, Primitive roundCone);
float sdBoundingBox(vec3 position, Primitive bBox, float thicness);

vec3 debugColor    = vec3(1,0,0);
bool useDebugColor = false;

float sdModel(vec3 position, int modelId) {

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

    // // optimize with dist to models bounding box

    // // data.y *= 0.5;
    // // model.bbMax *= model.scale;
    // // model.bbMin *= model.scale;

    // Primitive bb = {
    //     TYPE_BOX,
    //     OPERATION_ADD,
    //     0,
    //     model.transform,
    //     (model.bbMax - model.bbMin) * 0.5
    // };
    // finalDist = min(sdBoundingBox(
    //     (position - (model.bbMin.xyz * 0.5 + model.bbMax.xyz * 0.5)),
    //     bb,
    //     0.01
    // ), finalDist);
    // // finalDist = min(sdBox(position, bb), finalDist);

    return finalDist;
}

float sdPrimitive(vec3 position, Primitive primitive) {
    switch (primitive.type) {
        case TYPE_SPHERE:     return sdSphere(position, primitive);
        case TYPE_CAPSULE:    return sdCapsule(position, primitive);
        case TYPE_TORUS:      return sdTorus(position, primitive);
        case TYPE_BOX:        return sdBox(position, primitive);
        case TYPE_CILINDER:   return sdCilinder(position, primitive);
        case TYPE_CONE:       return sdCone(position, primitive);
        case TYPE_ROUND_CONE: return roundCone(position, primitive);
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
    float w = cilinder.data.x - cilinder.data.z;
    float h = cilinder.data.y - cilinder.data.z;
    vec2  d = abs(vec2(length(p.xz), p.y)) - vec2(w, h);
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0)) - cilinder.data.z;
}

float sdCone(vec3 position, Primitive cone) {
    float r1 = cone.data.x - cone.data.w;
    float r2 = cone.data.y - cone.data.w;
    float h  = cone.data.z - cone.data.w;
    vec3  p  = TRANSFORM_POS(position, cone);

    vec2 q = vec2( length(p.xz), p.y );
    vec2 k1 = vec2(r2,h);
    vec2 k2 = vec2(r2-r1,2.0*h);
    vec2 ca = vec2(q.x-min(q.x,(q.y<0.0)?r1:r2), abs(q.y)-h);
    vec2 cb = q - k1 + k2*clamp( dot(k1-q,k2)/dot(k2.xy, k2.xy), 0.0, 1.0 );
    float s = (cb.x<0.0 && ca.y<0.0) ? -1.0 : 1.0;
    return s*sqrt( min(dot(ca.xy, ca.xy),dot(cb.xy, cb.xy)) ) - cone.data.w;
}

float roundCone(vec3 position, Primitive roundCone) {
    float r1 = roundCone.data.x;
    float r2 = roundCone.data.y;
    float h  = roundCone.data.z;
    vec3  p  = TRANSFORM_POS(position, roundCone);
    p.y += h * 0.5;

    vec2 q = vec2( length(p.xz), p.y );

    float b = (r1-r2)/h;
    float a = sqrt(1.0-b*b);
    float k = dot(q,vec2(-b,a));

    if( k < 0.0 ) return length(q) - r1;
    if( k > a*h ) return length(q-vec2(0.0,h)) - r2;

    return dot(q, vec2(a,b) ) - r1;
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
