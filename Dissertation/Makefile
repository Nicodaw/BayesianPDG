LATEXMK = latexmk
RM = rm -f

DOCNAME = Dissertation

all: doc
doc: pdf
pdf: $(DOCNAME).pdf

%.pdf: %.tex
	$(LATEXMK) -pdf -M -MP -MF build/$*.d $*

mostlyclean:
	$(LATEXMK) -silent -c
	$(RM) *.bbl

clean: mostlyclean
	$(LATEXMK) -silent -C
	$(RM) *.run.xml
	$(RM) *.d

.PHONY: all clean doc mostlyclean pdf

-include build/*.d

