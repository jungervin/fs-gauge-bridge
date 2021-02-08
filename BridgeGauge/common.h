#pragma once

#ifndef _COMMON_H_
#define _COMMON_H_

#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wunused-function"
#pragma GCC diagnostic ignored "-Wignored-attributes"
#include <MSFS\MSFS.h>
#include <MSFS\MSFS_Render.h>
#include <MSFS\Legacy\gauges.h>
#include <SimConnect.h>
#pragma GCC diagnostic pop

#include <stdio.h>
#include <string.h>
#include <math.h>
#include <cassert>
#include <exception>
#include <iostream>

#ifndef __INTELLISENSE__
#	define MODULE_EXPORT __attribute__( ( visibility( "default" ) ) )
#	define MODULE_WASM_MODNAME(mod) __attribute__((import_module(mod)))
#else
#	define MODULE_EXPORT
#	define MODULE_WASM_MODNAME(mod)
#	define __attribute__(x)
#	define __restrict__
#endif

#ifdef _MSC_VER
#define snprintf _snprintf_s
#elif !defined(__MINGW32__)
#include <iconv.h>
#endif

void log(std::string value) {
	std::cout << "Bridge WASM: " << value << std::endl;
}

#endif
