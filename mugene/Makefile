
EXTRA_DIST =  Makefile

# Warning: This is an automatically generated file, do not edit!

srcdir=.
top_srcdir=..

include $(top_srcdir)/config.make

ifeq ($(CONFIG),DEBUG)
ASSEMBLY_COMPILER_COMMAND = mcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/mugene.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../bin/Debug/mugenelib.dll
BUILD_DIR = bin/Debug

MUGENELIB_DLL_SOURCE=../bin/Debug/mugenelib.dll
DEFAULT_MACRO_MML_SOURCE=../mml/default-macro.mml
DRUM_PART_MML_SOURCE=../mml/drum-part.mml
GS_SYSEX_MML_SOURCE=../mml/gs-sysex.mml
NRPN_GS_XG_MML_SOURCE=../mml/nrpn-gs-xg.mml
VSQ_SUPPORT_MML_SOURCE=../mml/vsq-support.mml
MUGENE_EXE_MDB_SOURCE=bin/Debug/mugene.exe.mdb
MUGENE_EXE_MDB=$(BUILD_DIR)/mugene.exe.mdb

endif

ifeq ($(CONFIG),RELEASE)
ASSEMBLY_COMPILER_COMMAND = mcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = bin/Release/mugene.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../bin/Release/mugenelib.dll
BUILD_DIR = bin/Release

MUGENELIB_DLL_SOURCE=../bin/Release/mugenelib.dll
DEFAULT_MACRO_MML_SOURCE=../mml/default-macro.mml
DRUM_PART_MML_SOURCE=../mml/drum-part.mml
GS_SYSEX_MML_SOURCE=../mml/gs-sysex.mml
NRPN_GS_XG_MML_SOURCE=../mml/nrpn-gs-xg.mml
VSQ_SUPPORT_MML_SOURCE=../mml/vsq-support.mml
MUGENE_EXE_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(MUGENELIB_DLL) \
	$(MUGENE_EXE_MDB)  

PROGRAMFILES_MML = \
	$(DEFAULT_MACRO_MML) \
	$(DRUM_PART_MML) \
	$(GS_SYSEX_MML) \
	$(NRPN_GS_XG_MML) \
	$(VSQ_SUPPORT_MML)  

BINARIES = \
	$(MUGENE)  


RESGEN=resgen2

MUGENELIB_DLL = $(BUILD_DIR)/mugenelib.dll
DEFAULT_MACRO_MML = $(BUILD_DIR)/mml/default-macro.mml
DRUM_PART_MML = $(BUILD_DIR)/mml/drum-part.mml
GS_SYSEX_MML = $(BUILD_DIR)/mml/gs-sysex.mml
NRPN_GS_XG_MML = $(BUILD_DIR)/mml/nrpn-gs-xg.mml
VSQ_SUPPORT_MML = $(BUILD_DIR)/mml/vsq-support.mml
MUGENE = $(BUILD_DIR)/mugene

FILES = \
	driver.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	../MMLspec.txt \
	../LICENSE \
	mugene.in 

REFERENCES =  \
	System \
	System.Core

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(PROGRAMFILES_MML) $(BINARIES) 

#Targets
all: $(ASSEMBLY) $(PROGRAMFILES) $(PROGRAMFILES_MML) $(BINARIES)  $(top_srcdir)/config.make

include $(top_srcdir)/Makefile.include
#include $(srcdir)/custom-hooks.make



$(eval $(call emit-deploy-target,MUGENELIB_DLL))
$(eval $(call emit-deploy-target,DEFAULT_MACRO_MML))
$(eval $(call emit-deploy-target,DRUM_PART_MML))
$(eval $(call emit-deploy-target,GS_SYSEX_MML))
$(eval $(call emit-deploy-target,NRPN_GS_XG_MML))
$(eval $(call emit-deploy-target,VSQ_SUPPORT_MML))
$(eval $(call emit-deploy-wrapper,MUGENE,mugene,x))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'


$(ASSEMBLY_MDB): $(ASSEMBLY)
$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	make pre-all-local-hook prefix=$(prefix)
	mkdir -p $(shell dirname $(ASSEMBLY))
	make $(CONFIG)_BeforeBuild
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
	make $(CONFIG)_AfterBuild
	make post-all-local-hook prefix=$(prefix)

install-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-install-local-hook prefix=$(prefix)
	make install-satellite-assemblies prefix=$(prefix)
	mkdir -p '$(DESTDIR)$(libdir)/$(PACKAGE)'
	$(call cp,$(ASSEMBLY),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(ASSEMBLY_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(MUGENELIB_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	mkdir -p '$(DESTDIR)$(libdir)/$(PACKAGE)/mml'
	$(call cp,$(DEFAULT_MACRO_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call cp,$(DRUM_PART_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call cp,$(GS_SYSEX_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call cp,$(NRPN_GS_XG_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call cp,$(VSQ_SUPPORT_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call cp,$(MUGENE_EXE_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	mkdir -p '$(DESTDIR)$(bindir)'
	$(call cp,$(MUGENE),$(DESTDIR)$(bindir))
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-uninstall-local-hook prefix=$(prefix)
	make uninstall-satellite-assemblies prefix=$(prefix)
	$(call rm,$(ASSEMBLY),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(ASSEMBLY_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(MUGENELIB_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(DEFAULT_MACRO_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call rm,$(DRUM_PART_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call rm,$(GS_SYSEX_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call rm,$(NRPN_GS_XG_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call rm,$(VSQ_SUPPORT_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call rm,$(MUGENE_EXE_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(MUGENE),$(DESTDIR)$(bindir))
	make post-uninstall-local-hook prefix=$(prefix)

install: install-local
uninstall: uninstall-local
clean: clean-local

include $(top_srcdir)/rules.make
