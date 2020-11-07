cmake_minimum_required (VERSION 3.18)

if(NOT DEFINED ${${PROJECT_NAME}_RESOURCES})
    if(NOT DEFINED ${DEFAULT_RESOERCES_PATH})
        set(DEFAULT_RESOERCES_PATH "${CMAKE_CURRENT_LIST_DIR}/resources")
    endif()
    set(${PROJECT_NAME}_RESOURCES  "${DEFAULT_RESOERCES_PATH}" CACHE PATH "Relative or absolute path to Application resources.")
endif()

# generate shader definitions from SHADER_FILES based on their location
set(SHADER_DEBUG_COMPILE_DEFINITIONS )
set(SHADER_RELEASE_COMPILE_DEFINITIONS )
foreach(relative_file_name ${SHADER_FILES})

   unset(shader_file CACHE)

    # find full name of shader
    find_file(shader_file
        ${relative_file_name}
        HINTS ${${PROJECT_NAME}_RESOURCES}/shaders/
    )

    # create shader definition string
    string(REPLACE "/" "_" definition_name ${relative_file_name})
    string(REPLACE "\\" "_" definition_name ${definition_name})
    string(REGEX REPLACE "\\..*" "" definition_name ${definition_name})
    string(TOUPPER ${definition_name} definition_name)

    string(CONCAT shader_definition
        "SHADER_" ${definition_name} "=\"" ${shader_file} "\""
    )

    string(CONCAT shader_definition_release
        "SHADER_" ${definition_name} "=\"resources/shaders/" ${relative_file_name} "\""
    )

    # json configuration print out for vs code
    message(
        "\"SHADER_" ${definition_name} "=\\\"" ${shader_file} "\\\"\","
    )

    # message(${shader_definition_release})

    # add shader_definition to SHADER_DEBUG_COMPILE_DEFINITIONS list
    list(APPEND SHADER_DEBUG_COMPILE_DEFINITIONS ${shader_definition})
    list(APPEND SHADER_RELEASE_COMPILE_DEFINITIONS ${shader_definition_release})
endforeach()
