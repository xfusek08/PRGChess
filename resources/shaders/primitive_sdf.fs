#version 460 core

// see https://iquilezles.org/www/articles/distfunctions/distfunctions.htm

#define MAX_STEPS    100
#define MAX_DISTANCE 100.0
#define HIT_DISTANCE 0.01

#define TYPE_SPHERE   0
#define TYPE_CAPSULE  1
#define TYPE_TORUS    2
#define TYPE_BOX      3
#define TYPE_CILINDER 4

struct Primitive {
    int type;
    vec4 position; // todo: use transform instead of position
    float data[4];
};

// here scene uniform buffer will be loaded
Primitive scene[] = {
    { TYPE_SPHERE,   vec4( 0.0, 2.0,  0.0,  1.0), { 1.0, 0.0, 0.0, 0.0 } },
    { TYPE_CAPSULE,  vec4( 5.0, 4.0, -1.0,  1.0), { 2.0, 0.3, 0.0, 0.0 } },
    { TYPE_TORUS,    vec4( 2.0, 2.0,  0.0,  1.0), { 4.0, 0.4, 0.0, 0.0 } },
    { TYPE_BOX,      vec4( 3.0, 3.0, -0.5, -1.0), { 1.0, 0.5, 1.0, 0.0 } },
    { TYPE_BOX,      vec4(-3.0, 3.0, -0.5, -1.0), { 1.0, 0.5, 1.0, 0.1 } },
    { TYPE_CILINDER, vec4( 5.0, 4.0,  5.0,  1.0), { 2.0, 0.5, 0.0, 0.0 } },
};

int sceneSize = 6;

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
            case TYPE_BOX:      actDist = sdBox(position, scene[i]);       break;
            case TYPE_CILINDER: actDist = sdCilinder(position, scene[i]); break;
        }

        if (actDist <= HIT_DISTANCE) {
            return actDist;
        }

        minDist = min(actDist, minDist);
    }
    return min(minDist, position.y);
}

float sdSphere(vec3 position, Primitive sphere) {
    return length(position - sphere.position.xyz) - sphere.data[0];
}

float sdCapsule(vec3 position, Primitive capsule) {
    float height = capsule.data[0];
    float width = capsule.data[1];
    vec3 a = vec3(0,  0.5, 0) * height + capsule.position.xyz;
    vec3 b = vec3(0, -0.5, 0) * height + capsule.position.xyz;
    vec3 ab = b - a;
    vec3 ap = position - a;
    float t = clamp(dot(ab, ap) / dot(ab, ab), 0, 1);
    return length(ap - ab * t) - width;
}

float sdTorus(vec3 position, Primitive torus) {
    vec3 p = position - torus.position.xyz;
    float x = length(p.xz) - torus.data[0];
    return length(vec2(x, p.y)) - torus.data[1];
}

float sdBox(vec3 position, Primitive box) {
    vec3  p = position - box.position.xyz;
    vec3  d = abs(p) - vec3(box.data[0], box.data[1], box.data[2]);
    float e = length(max(d, 0.0));               // exterior distance
    float i = min(max(d.x, max(d.y, d.z)), 0.0); // interior distance
    return e + i - box.data[3];
}

float sdCilinder(vec3 position, Primitive cilinder) {
    vec3  p = position - cilinder.position.xyz;
    float h = cilinder.data[0];
    float w = cilinder.data[1];
    vec2  d = abs(vec2(length(p.xz), p.y)) - vec2(h, w);
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
}
