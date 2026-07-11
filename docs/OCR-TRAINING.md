# OCR Training

## Adaptive Learning

Immediate learning stores correction mappings and vocabulary in:

```text
D:\INVENTORY_PHOTO_OPS\ocr-training\ocr-learning.json
D:\INVENTORY_PHOTO_OPS\ocr-training\tesseract-user-words.txt
```

Corrections are field-aware and applied with token-boundary matching rather than unrestricted global substitution.

## Formal Tesseract Training

The .NET rewrite does not automatically fine-tune Tesseract LSTM models. Future training tooling should validate Tesseract training dependencies, generate `.lstmf` files, evaluate a model, and require explicit approval before activating a `.traineddata` file.

