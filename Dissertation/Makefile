LATEXMK = latexmk
RM = rm -f

DOCNAME = Dissertation

all: batch
batch: LIVE_ARG=
continuous: LIVE_ARG=-pvc

batch continuous: $(DOCNAME).pdf

%.pdf: %.tex
	$(LATEXMK) $(LIVE_ARG) -pdf -M -MP -MF build/$*.d $*

mostlyclean:
	$(LATEXMK) -silent -c
	$(RM) *.bbl

clean: mostlyclean
	$(LATEXMK) -silent -C
	$(RM) *.run.xml
	$(RM) *.d

.PHONY: all clean mostlyclean batch continuous

-include build/*.d

