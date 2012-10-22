include ../Makefile.mk

all: XInstall.Util.dll

clean:
	rm -f ./bin/*.dll


XInstall.Util.dll: *.cs
	$(GMCS) -target:library -out:$(util_bin)/$@ $(CSFILES)

