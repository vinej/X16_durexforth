# durexForth for the Commander X16.
#
# The build is driven by build.sh (assemble with ACME + write the sources
# into the FAT32 SD-card image). See build.sh for the requirements.

all:
	./build.sh

run:
	./build.sh run

clean:
	rm -f durexforth.prg emulator/durexforth.prg
	rm -rf build/*.pet
