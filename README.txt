VVVV Patch files for Kimchi and Chips' installation Link

Operating notes:
================

If you're on a slow graphics card, switch off bicubic filtering in the shader (e.g. Matrox cards)


NOTE : FileTexture DX11 needs to be customised to allow for spreaded reload flag:

https://gist.github.com/elliotwoods/62c5524c89ffd7fc8d14

We'll try and switch to Pooled Texture2D to avoid needing this switch.

Installation
============

### VVVV

* Install latest vvvv x86 and vvvv x64
* Install vvvvv-dx11 for x64
* Install http://vvvv.org/contribution/vvvv.packs.image for x86
* Install https://github.com/elliotwoods/VVVV.Packs.KimchiAndChips.git for both
* Build vvvv-dx11 with above patch

### MySQL

* Install MySQL with MySQL workbench
* Pump the data from https://gist.github.com/elliotwoods/d2e039f1f59ba3ba023f into a local schema 'link'
* Create user with access to link schema
