cmake_minimum_required(VERSION 3.0)
project(json)

# set_target_properties(json PROPERTIES LINKER_LANGUAGE CXX)

add_library(json INTERFACE)
target_include_directories(json INTERFACE include/)
