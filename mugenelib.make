

# Warning: This is an automatically generated file, do not edit!

srcdir=.
top_srcdir=.

include $(top_srcdir)/config.make

ifeq ($(CONFIG),DEBUG)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/mugenelib.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Debug

DEFAULT_MACRO_MML_SOURCE=mml/default-macro.mml
DRUM_PART_MML_SOURCE=mml/drum-part.mml
GS_SYSEX_MML_SOURCE=mml/gs-sysex.mml
NRPN_GS_XG_MML_SOURCE=mml/nrpn-gs-xg.mml
VSQ_SUPPORT_MML_SOURCE=mml/vsq-support.mml
MONO_C5_DLL_SOURCE=bin/Debug/Mono.C5.dll
MONO_C5_DLL_MDB_SOURCE=bin/Debug/Mono.C5.dll.mdb
MUGENELIB_DLL_MDB_SOURCE=bin/Debug/mugenelib.dll.mdb
MUGENELIB_DLL_MDB=$(BUILD_DIR)/mugenelib.dll.mdb

endif

ifeq ($(CONFIG),RELEASE)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = bin/Release/mugenelib.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release

DEFAULT_MACRO_MML_SOURCE=mml/default-macro.mml
DRUM_PART_MML_SOURCE=mml/drum-part.mml
GS_SYSEX_MML_SOURCE=mml/gs-sysex.mml
NRPN_GS_XG_MML_SOURCE=mml/nrpn-gs-xg.mml
VSQ_SUPPORT_MML_SOURCE=mml/vsq-support.mml
MONO_C5_DLL_SOURCE=bin/Debug/Mono.C5.dll
MONO_C5_DLL_MDB_SOURCE=bin/Debug/Mono.C5.dll.mdb
MUGENELIB_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES_MML = \
	$(DEFAULT_MACRO_MML) \
	$(DRUM_PART_MML) \
	$(GS_SYSEX_MML) \
	$(NRPN_GS_XG_MML) \
	$(VSQ_SUPPORT_MML)  

PROGRAMFILES = \
	$(MONO_C5_DLL) \
	$(MONO_C5_DLL_MDB) \
	$(MUGENELIB_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(MUGENELIB_PC)  


RESGEN=resgen2

DEFAULT_MACRO_MML = $(BUILD_DIR)/mml/default-macro.mml
DRUM_PART_MML = $(BUILD_DIR)/mml/drum-part.mml
GS_SYSEX_MML = $(BUILD_DIR)/mml/gs-sysex.mml
NRPN_GS_XG_MML = $(BUILD_DIR)/mml/nrpn-gs-xg.mml
VSQ_SUPPORT_MML = $(BUILD_DIR)/mml/vsq-support.mml
MONO_C5_DLL = $(BUILD_DIR)/Mono.C5.dll
MONO_C5_DLL_MDB = $(BUILD_DIR)/Mono.C5.dll.mdb
MUGENELIB_PC = $(BUILD_DIR)/mugenelib.pc

FILES = \
	src/SMF.cs \
	src/mml_variable_processor.cs \
	src/mml_tokenizer.cs \
	src/mml_smf_generator.cs \
	src/mml_semantic_builder.cs \
	src/mml_parser.cs \
	src/mml_macro_expander.cs \
	src/mml_compiler_main.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	mml/default-macro.mml \
	mml/drum-part.mml \
	mml/gs-sysex.mml \
	mml/nrpn-gs-xg.mml \
	mml/vsq-support.mml \
	src/mml_parser.jay \
	src \
	mugenelib.pc.in 

REFERENCES =  \
	System.Core \
	System

DLL_REFERENCES =  \
	bin/Debug/Mono.C5.dll

CLEANFILES = $(PROGRAMFILES_MML) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

#Targets
all-local: $(ASSEMBLY) $(PROGRAMFILES_MML) $(PROGRAMFILES) $(LINUX_PKGCONFIG)  $(top_srcdir)/config.make



$(eval $(call emit-deploy-target,DEFAULT_MACRO_MML))
$(eval $(call emit-deploy-target,DRUM_PART_MML))
$(eval $(call emit-deploy-target,GS_SYSEX_MML))
$(eval $(call emit-deploy-target,NRPN_GS_XG_MML))
$(eval $(call emit-deploy-target,VSQ_SUPPORT_MML))
$(eval $(call emit-deploy-target,MONO_C5_DLL))
$(eval $(call emit-deploy-target,MONO_C5_DLL_MDB))
$(eval $(call emit-deploy-wrapper,MUGENELIB_PC,mugenelib.pc))


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
	mkdir -p '$(DESTDIR)$(libdir)/$(PACKAGE)/mml'
	$(call cp,$(DEFAULT_MACRO_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call cp,$(DRUM_PART_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call cp,$(GS_SYSEX_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call cp,$(NRPN_GS_XG_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call cp,$(VSQ_SUPPORT_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call cp,$(MONO_C5_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(MONO_C5_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(MUGENELIB_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	mkdir -p '$(DESTDIR)$(libdir)/pkgconfig'
	$(call cp,$(MUGENELIB_PC),$(DESTDIR)$(libdir)/pkgconfig)
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-uninstall-local-hook prefix=$(prefix)
	make uninstall-satellite-assemblies prefix=$(prefix)
	$(call rm,$(ASSEMBLY),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(ASSEMBLY_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(DEFAULT_MACRO_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call rm,$(DRUM_PART_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call rm,$(GS_SYSEX_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call rm,$(NRPN_GS_XG_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call rm,$(VSQ_SUPPORT_MML),$(DESTDIR)$(libdir)/$(PACKAGE)/mml)
	$(call rm,$(MONO_C5_DLL),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(MONO_C5_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(MUGENELIB_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(MUGENELIB_PC),$(DESTDIR)$(libdir)/pkgconfig)
	make post-uninstall-local-hook prefix=$(prefix)
