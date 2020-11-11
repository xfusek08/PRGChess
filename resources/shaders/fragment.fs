#version 460 core

in vec2 fragCoord;
out vec4 fColor;

uniform float aspectRatio;
uniform float viewDistance;
uniform mat4 cameraOrientation;
uniform vec3 cameraPosition;

#define MAX_STEPS 100
#define MAX_DISTANCE 100.0
#define HIT_DISTANCE 0.1
#define SPHERE vec4(0, 1, 6, 1)

float getDistanceToScene(vec3 position) {
    vec4 sphere = SPHERE;

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

float getLight(vec3 point) {
    vec3 lightPos = vec3(1, 5, 6);
    vec3 l = normalize(lightPos - point);
    vec3 n = getNormal(point);

    float diff = dot(n, l);
    return diff;
}

float rayMarch(vec3 originPoint, vec3 direction) {

    float distanceMarched = 0;

    for (int step = 0; step < MAX_STEPS; ++step) {
        vec3 position = originPoint + distanceMarched * direction;
        float distanceToScene = getDistanceToScene(position);
        distanceMarched += distanceToScene;
        if (distanceMarched > MAX_DISTANCE || distanceMarched < HIT_DISTANCE) {
            break; // hit
        }
    }

    return distanceMarched;
}

void main(){
    // aspectRatio = 1.5;
    vec2 uv = fragCoord;
    uv.x = uv.x * aspectRatio;
    vec3 rayDirection = (cameraOrientation * vec4(normalize(vec3(uv.xy, -viewDistance)), 1)).xyz;
    // vec3 rayDirection = normalize(vec3(uv.xy, -viewDistance));

    float dist = rayMarch(cameraPosition, rayDirection);

    vec3 position = cameraPosition + rayDirection * dist;

    vec3 color = vec3(getLight(position));
    // vec3 color = rayDirection;
    fColor = vec4(color, 1);
}
