cmake_minimum_required(VERSION 3.22)
project(AnythingUIQt VERSION 0.1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_AUTOMOC ON)

find_package(Qt6 REQUIRED COMPONENTS Core Gui Widgets)

add_executable(AnythingUIQt
    src/main.cpp
    src/MainWindow.h
    src/MainWindow.cpp
    src/SearchEngineApi.h
    src/SearchEngineApi.cpp
    src/FileResultModel.h
    src/FileResultModel.cpp
)

target_link_libraries(AnythingUIQt PRIVATE Qt6::Core Qt6::Gui Qt6::Widgets)

# dwmapi is Windows-only (Mica + dark title bar)
if(WIN32)
    target_link_libraries(AnythingUIQt PRIVATE dwmapi)
endif()

# Copy the Rust library to the output directory
set(RUST_TARGET_DIR "${CMAKE_SOURCE_DIR}/../SearchEngine/target/release")

if(WIN32)
    set(RUST_LIB_NAME "searchengine.dll")
elseif(APPLE)
    set(RUST_LIB_NAME "libsearchengine.dylib")
else()
    set(RUST_LIB_NAME "libsearchengine.so")
endif()

add_custom_command(TARGET AnythingUIQt POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "${RUST_TARGET_DIR}/${RUST_LIB_NAME}"
        "$<TARGET_FILE_DIR:AnythingUIQt>/${RUST_LIB_NAME}"
)
