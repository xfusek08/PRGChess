#version 460 core

// see https://iquilezles.org/www/articles/distfunctions/distfunctions.htm

// settings should by same as in main fragment shader
#define MAX_STEPS    100
#define MAX_DISTANCE 100.0
#define HIT_DISTANCE 0.01
#define MAX_PRIMITIVES 32

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

layout (std140) uniform SceneBlock { Primitive scene[MAX_PRIMITIVES]; };
uniform uint sceneSize;

float smoothMin(float dist1, float dist2, float koeficient) {
    float h = clamp( 0.5 + 0.5 * (dist1 - dist2) / koeficient, 0.0, 1.0 );
    return mix(dist1, dist2, h ) - koeficient * h * (1.0-h);
}

float smoothMax(float dist1, float dist2, float koeficient) {
    float h = clamp( 0.5 - 0.5 * (dist1 - dist2) / koeficient, 0.0, 1.0 );
    return mix(dist1, dist2, h ) + koeficient * h * (1.0-h);
}

// SDF definitions
float sdPrimitive(vec3 position, Primitive prmiitive);
float sdSphere(vec3 position, Primitive sphere);
float sdCapsule(vec3 position, Primitive capsule);
float sdTorus(vec3 position, Primitive torus);
float sdBox(vec3 position, Primitive box);
float sdCilinder(vec3 position, Primitive cilinder);

// MAIN portal to different primitives
float sdToScene(vec3 position) {
    float finalDist = MAX_DISTANCE;
    for (int i = 0; i < sceneSize; ++i) {

        Primitive primitive = scene[i];
        float distToPrimitive = sdPrimitive(position, primitive);

        switch (primitive.operation) {
            case OPERATION_ADD:       finalDist = smoothMin(distToPrimitive, finalDist, primitive.blending); break;
            case OPERATION_SUBSTRACT: finalDist = smoothMax(-distToPrimitive, finalDist, primitive.blending); break;
            case OPERATION_INTERSECT: finalDist = smoothMax(distToPrimitive, finalDist, primitive.blending); break;
        };
    }
    return finalDist;
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

// cammon primitive calculations
vec3 primitivePositon(vec3 position, Primitive primitive) {
    return (primitive.transform * vec4(position, 1)).xyz;
}

// primitive SD functions

float sdSphere(vec3 position, Primitive sphere) {
    return length(primitivePositon(position, sphere)) - sphere.data.x;
}

float sdCapsule(vec3 position, Primitive capsule) {
    vec3 p = primitivePositon(position, capsule);
    vec3 a = vec3(0,  0.5, 0) * capsule.data.x;
    vec3 b = vec3(0, -0.5, 0) * capsule.data.x;
    vec3 ab = b - a;
    vec3 ap = p - a;
    float t = clamp(dot(ab, ap) / dot(ab, ab), 0, 1);
    return length(ap - ab * t) - capsule.data.y;
}

float sdTorus(vec3 position, Primitive torus) {
    vec3 p = primitivePositon(position, torus);
    float x = length(p.xz) - torus.data.x;
    return length(vec2(x, p.y)) - torus.data.y;
}

float sdBox(vec3 position, Primitive box) {
    vec3 p = primitivePositon(position, box);
    vec3  d = abs(p) - box.data.xyz;
    float e = length(max(d, 0.0));               // exterior distance
    float i = min(max(d.x, max(d.y, d.z)), 0.0); // interior distance
    return e + i - box.data.w;
}

float sdCilinder(vec3 position, Primitive cilinder) {
    vec3 p = primitivePositon(position, cilinder);
    float h = cilinder.data.x;
    float w = cilinder.data.y;
    vec2  d = abs(vec2(length(p.xz), p.y)) - vec2(h, w);
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
}
