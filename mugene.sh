MUGENE_INST_DIR=/svn/commons-music-prog/mugene
DIR=$MUGENE_INST_DIR

mono --debug $MONO_OPTIONS $DIR/bin/Debug/mugene.exe $DIR/mml/default-macro.mml $DIR/mml/gs-sysex.mml $DIR/mml/drum-part.mml $DIR/mml/nrpn-gs-xg.mml $@
