# YOLO-V1.2-WinML-Sample
Updated to work with the release preview of Win ML. 

Windows Machine Learning implementation using TinyYOLO (You-Only-Look-Once). For details on YOLO, check out the inventor's site - https://pjreddie.com/darknet/yolo/

# Acknowledgements
This sample project was based on work by https://elbruno.com/2018/06/28/winml-how-to-create-a-windows10-app-using-yolo-for-object-detection/ and https://github.com/sevans4067/WinMl-TinyYOLO

# Setup
To try out the pre-released Windows ML, you'll need the Windows 10 Insider Preview (Build 17728 or higher) and the Windows 10 SDK (Build 17723 or higher).

If running on desktop Windows, you'll need to set your configuration to x64.

# Model
The tiny-yolov2-1.2.onnx model embedded with this project was created from the model located at https://gallery.azure.ai/Model/Tiny-YOLOv2-1-2-3. This model represents YOLOv2 1.2, and was built off the classes in the Pascal VOC project. See https://github.com/onnx/onnxmltools for more information on how to convert models to ONNX.

Windows ML requires ONNX models, version 1.2 or higher - https://docs.microsoft.com/en-us/windows/ai/get-onnx-model
