#version 460 core

// see https://iquilezles.org/www/articles/distfunctions/distfunctions.htm

#define MAX_STEPS    50
#define MAX_DISTANCE 100.0
#define HIT_DISTANCE 0.01

#define MAX_PRIMITIVES 32

#define TYPE_SPHERE   0
#define TYPE_CAPSULE  1
#define TYPE_TORUS    2
#define TYPE_BOX      3
#define TYPE_CILINDER 4

struct Primitive {
    mat4 transform;
    vec4 data;
    uint type;
};

layout (std140) uniform SceneBlock { Primitive scene[MAX_PRIMITIVES]; };
uniform uint sceneSize;

// SDF definitions
float sdSphere(vec3 position, Primitive sphere);
float sdCapsule(vec3 position, Primitive capsule);
float sdTorus(vec3 position, Primitive torus);
float sdBox(vec3 position, Primitive box);
float sdCilinder(vec3 position, Primitive cilinder);

// MAIN portal to different primitives
float sdToScene(vec3 position) {
    float minDist = MAX_DISTANCE;
    for (int i = 0; i < sceneSize; ++i) {
        float actDist = MAX_DISTANCE;
        switch (scene[i].type) {
            case TYPE_SPHERE:   actDist = sdSphere(position, scene[i]);   break;
            case TYPE_CAPSULE:  actDist = sdCapsule(position, scene[i]);  break;
            case TYPE_TORUS:    actDist = sdTorus(position, scene[i]);    break;
            case TYPE_BOX:      actDist = sdBox(position, scene[i]);      break;
            case TYPE_CILINDER: actDist = sdCilinder(position, scene[i]); break;
        }

        if (actDist <= HIT_DISTANCE) {
            return actDist;
        }

        minDist = min(actDist, minDist);
    }
    return min(minDist, position.y);
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
