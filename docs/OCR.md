# OCR

OCR uses the configured Tesseract executable, defaulting to:

```text
E:\Apps\tesseract-ocr\tesseract.exe
```

The OCR service runs multiple page segmentation modes, parses TSV output, stores multiple candidates, and scores quality before choosing merged text.

Current profiles include trading-card front/back, book cover, card/serial number, full image, and auto-style fallback plans.

The .NET OCR pipeline now generates working-copy image variants before invoking Tesseract. Variants include EXIF auto-orient output, detected card-boundary crops, custom crop regions, grayscale/contrast/sharpened copies, and thresholded copies. Originals are not overwritten.

Each OCR candidate stores derived image path, crop rectangle JSON, rotation, transform metadata, raw TSV, word boxes, confidence, and candidate score.
