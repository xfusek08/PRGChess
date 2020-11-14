#version 460 core

in vec2 fragCoord;
out vec4 fColor;

// scene
uniform vec4 sphere;
uniform vec3 lightPosition;
uniform float planeHeight;

// camera
uniform float aspectRatio;
uniform float cameraFOVDegrees;
uniform vec3  cameraPosition;
uniform vec3  cameraDirection;
uniform vec3  cameraUp;

#define MAX_STEPS    100
#define MAX_DISTANCE 1000.0
#define HIT_DISTANCE 0.01

float getDistanceToScene(vec3 position) {
    float distanceToSphere = length(position - sphere.xyz) - sphere.w;

    float planeDistance = position.y;

    return min(distanceToSphere, planeDistance);
}

vec3 getNormal(vec3 point) {
    float d = getDistanceToScene(point);
    vec2 e = vec2(0.01, 0);
    vec3 n = d - vec3(
        getDistanceToScene(point-e.xyy),
        getDistanceToScene(point-e.yxy),
        getDistanceToScene(point-e.yyx)
    );

    return normalize(n);
}

float rayMarch(vec3 originPoint, vec3 direction) {
    float distanceMarched = 0;
    for (int step = 0; step < MAX_STEPS; ++step) {
        vec3 position = originPoint + distanceMarched * direction;
        distanceMarched += getDistanceToScene(position);
        if (distanceMarched > MAX_DISTANCE || distanceMarched < HIT_DISTANCE) {
            break; // hit
        }
    }
    return distanceMarched;
}

float getLight(vec3 point) {
    vec3 l = normalize(lightPosition - point);
    vec3 n = getNormal(point);

    float diff = clamp(dot(n, l), 0.0, 1.0);
    float distToLight = rayMarch(point + n * HIT_DISTANCE * 2, l);

    if (distToLight < length(lightPosition - point)) {
        return diff * 0.1;
    }
    return diff;
}

vec3 computeRayDirection(vec2 uv) {
    // return normalize(vec3(uv.xy, 1)); // marching forwards

    vec3 left = normalize(cross(cameraUp, cameraDirection));
    vec3 upLocal = normalize(cross(cameraDirection, left));
    float h = tan((3.141592 * cameraFOVDegrees) / 180);
    float w = h * aspectRatio;
    return normalize(cameraDirection + upLocal * h * uv.y + left * w * uv.x);
}

void main(){
    // aspectRatio = 1.5;
    vec2 uv = fragCoord;

    vec3 rayDirection = computeRayDirection(uv);

    float dist = rayMarch(cameraPosition, rayDirection);

    vec3 position = cameraPosition + rayDirection * dist;

    vec3 color;

    // axes
    if (dist < (MAX_DISTANCE / 2) - 1 && abs(position.x) < 0.05) {
        if (position.z > 0 ) {
            color = vec3(0.4, 0.7, 1.0); // z axis positive
        } else {
            color = vec3(0.1, 0.1, 1.0); // z axis negative
        }
    } else if (dist < (MAX_DISTANCE / 2) - 1 && abs(position.z) < 0.05) {
        if (position.x > 0 ) {
            color = vec3(1, 0.1, 0.1); // x axis positive
        } else {
            color = vec3(1, 0.7, 0.4); // x axis negative
        }
    } else {
        color = vec3(getLight(position));
    }

    // vec3 color = rayDirection;
    // vec3 color = vec3(length(uv));
    fColor = vec4(color, 1);
}
