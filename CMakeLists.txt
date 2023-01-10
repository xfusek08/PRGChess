cmake_minimum_required (VERSION 3.18)
project (PRGChess)

set(CMAKE_CXX_STANDARD 17)

# set(CMAKE_CXX_CLANG_TIDY "clang-tidy;-checks=*")

set(SOURCES
    src/main.cpp
    src/sceneUtils.h src/sceneUtils.cpp
    src/AABB.h src/AABB.cpp

    # scene
    src/scene/Transform.h src/scene/Transform.cpp
    src/scene/Primitive.h src/scene/Primitive.cpp
    src/scene/ModelGeometry.h
    src/scene/Material.h
    src/scene/Model.h
    src/scene/Scene.h
)

add_subdirectory(vendor/RenderBase)
add_subdirectory(vendor/json)

add_executable(${PROJECT_NAME} ${SOURCES})
target_link_libraries(${PROJECT_NAME} RenderBase json)
target_include_directories(${PROJECT_NAME} PUBLIC src)

# Load Resource file paths definitions
include(vendor/RenderBase/cmakeUtils/LoadResourceFiles.cmake)
load_resource_definitions(resources RESOURCES_DEBUG_DEFINITIONS RESOURCES_RELEASE_DEFINITIONS)

target_compile_definitions(${PROJECT_NAME} PUBLIC
    "PROJECT_NAME=\"${PROJECT_NAME}\""
    $<$<CONFIG:Debug>:DEBUG ${RESOURCES_DEBUG_DEFINITIONS}>
    $<$<CONFIG:Release>:NDEBUG ${RESOURCES_RELEASE_DEFINITIONS}>
)

# Ensure that on release build we copy resources to build directory because release build is using relative path to resources
# Implementation based on:
#   https://stackoverflow.com/a/46439715
# and on
#   https://stackoverflow.com/a/13429998
set(no_copy $<NOT:$<CONFIG:Release>>)
add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
    COMMAND "${CMAKE_COMMAND}" -E
        $<${no_copy}:echo>
        $<${no_copy}:"copy omitted for non-release build, command would have been ">
        remove_directory $<TARGET_FILE_DIR:${PROJECT_NAME}>/resources
    COMMAND "${CMAKE_COMMAND}" -E
        $<${no_copy}:echo>
        $<${no_copy}:"copy omitted for non-release build, command would have been ">
        copy_directory ${CMAKE_SOURCE_DIR}/resources $<TARGET_FILE_DIR:${PROJECT_NAME}>/resources
)
