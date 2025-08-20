# Project Overview: Piper-Vivox TTS Bridge

This project integrates the **Piper Unity Sample** with the **Vivox SDK** to create a system that transmits real-time Text-to-Speech (TTS) audio into a Vivox voice channel within the Unity environment. This allows users to type text and have the generated speech heard by other participants in the same Vivox channel.

## Key Features

  * **Real-Time TTS Conversion**: Utilizes Piper, a lightweight, on-device TTS system, to convert text into high-quality voice audio. Piper operates offline and uses compact models (20-60MB).
  * **Dynamic Voice Model Selection**: A Unity UI dropdown enables users to easily switch between various Piper voice models (.sentis) at runtime.
  * **Vivox Voice Channel Integration**: The generated TTS audio is set as the virtual microphone input for Vivox, allowing it to be transmitted to the voice channel in real time.
  * **Cross-Platform Compatibility**: The TTS functionality is supported on multiple platforms, including Windows, macOS, and Android, as supported by Piper Unity.

## System Architecture

1.  **Text Input**: The user inputs text through a Unity UI element.
2.  **Piper TTS Processing**: The input text is processed by Piper's `ESpeakTokenizer` to generate phonemes, which are then synthesized into audio data by the selected `.sentis` voice model.
3.  **Audio Data Conversion**: The audio data from Piper is converted into a format compatible with Unity's `AudioClip` or a byte array.
4.  **Vivox Microphone Input Setup**: The converted audio data is set as a virtual input device using the Vivox SDK's `SetAudioInputDevice` function.
5.  **Voice Channel Transmission**: Vivox streams the configured audio input in real time to all users participating in the voice channel.

## Development Environment and Requirements

  * **Unity**: `6000.2.0b9` or higher
  * **Piper Unity Sample**: The foundational [piper-unity repository](https://github.com/skykim/piper-unity) by Sky Kim.
  * **Vivox SDK for Unity**: Required for implementing Vivox voice channel functionality.

## Getting Started

1.  **Install the Vivox SDK**: Add the Vivox SDK to your project via the Unity Package Manager.
2.  **Integrate the Piper Unity Sample**: Set up the Piper Unity Sample project using this project's code. (Refer to the `Getting Started` section in the original `README.md` for base setup).
3.  **Configure Vivox Account**: Create an account on the Vivox Developer Portal, get your App ID and key, and configure them in your project.
4.  **Add Vivox-Piper Integration Scripts**: Write a custom script to connect to a Vivox channel and set the audio input to the Piper TTS output.
5.  **Run the Demo**: Run the project in the Unity editor. Connect to a Vivox channel, input some text, and confirm that the TTS audio is transmitted to the channel.

-----

### Contribution

This project is built upon the excellent Piper Unity Sample created by Sky Kim. A sincere thank you to the original repository and its developer for providing the foundation for this project.

Special thanks to Woojin Park, whose inspiration led to the completion of this project.