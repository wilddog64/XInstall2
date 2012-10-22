SRC     = .
CSFILES = $(wildcard *.cs)

util    = $(SRC)/Util
core    = $(SRC)/Core
actions = $(SRC)/Actions

util_bin    = $(util)/bin
core_bin    = $(core)/bin
actions_bin = $(actions)/bin

util_csfiles    = $(util)/$(CSFILES)
core_csfiles    = $(core)/$(CSFILES)
actions_csfiles = $(actions)/$(CSFILES)

Util.Lib    = $(util_bin)/XInstall.Util.dll
Core.Lib    = $(core_bin)/XInstall.Core.dll
Actions.Lib = $(actions_bin)/XInstall.Actions.dll

GMCS=gmcs
