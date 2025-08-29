#!/usr/bin/env python3
"""
Music Embeddings Wrapper Script

This script provides a bridge between the .NET Core backend and the music-embeddings Python module.
It extracts audio embeddings and returns them in JSON format.

Created by: Sergie Code - Software Engineer & YouTube Programming Educator
AI Tools for Musicians Series
"""

import sys
import json
import os
import traceback
from pathlib import Path

# Add the music-embeddings module to the Python path
# Adjust these paths based on your actual repository structure
EMBEDDINGS_REPO_PATH = os.path.join(os.path.dirname(__file__), "..", "..", "copyright-detector-music-embeddings")
VECTOR_SEARCH_REPO_PATH = os.path.join(os.path.dirname(__file__), "..", "..", "copyright-detector-vector-search")

sys.path.insert(0, os.path.join(EMBEDDINGS_REPO_PATH, "src"))

try:
    # Import from the music-embeddings module
    from embeddings import AudioEmbeddingExtractor
    from utils import validate_audio_file
    import numpy as np
    import librosa
except ImportError as e:
    # Fallback if modules are not available
    print(json.dumps({
        'success': False,
        'error': f'Failed to import required modules: {str(e)}',
        'embeddings': [],
        'shape': [],
        'model': '',
        'duration': 0,
        'sample_rate': 0
    }))
    sys.exit(1)

def extract_embeddings(audio_path, model_name='spectrogram'):
    """
    Extract embeddings from an audio file using the specified model
    
    Args:
        audio_path (str): Path to the audio file
        model_name (str): Model to use for extraction ('spectrogram', 'openl3', 'audioclip')
    
    Returns:
        dict: Result dictionary with embeddings and metadata
    """
    try:
        # Validate input file
        if not os.path.exists(audio_path):
            return {
                'success': False,
                'error': f'Audio file not found: {audio_path}',
                'embeddings': [],
                'shape': [],
                'model': model_name,
                'duration': 0,
                'sample_rate': 0
            }
        
        # Validate audio file format
        if not validate_audio_file(audio_path):
            return {
                'success': False,
                'error': f'Invalid audio file format: {audio_path}',
                'embeddings': [],
                'shape': [],
                'model': model_name,
                'duration': 0,
                'sample_rate': 0
            }
        
        # Get audio metadata
        try:
            y, sr = librosa.load(audio_path, sr=None)
            duration = len(y) / sr
        except Exception as e:
            duration = 0
            sr = 22050  # Default sample rate
        
        # Initialize the embedding extractor
        extractor = AudioEmbeddingExtractor(
            model_name=model_name,
            sample_rate=22050,  # Standard sample rate for consistency
            segment_duration=30  # 30-second segments
        )
        
        # Extract embeddings
        embeddings = extractor.extract_embeddings(audio_path)
        
        # Convert numpy array to list for JSON serialization
        if isinstance(embeddings, np.ndarray):
            embeddings_list = embeddings.tolist()
            shape = list(embeddings.shape)
        else:
            embeddings_list = []
            shape = []
        
        return {
            'success': True,
            'embeddings': embeddings_list,
            'shape': shape,
            'model': model_name,
            'duration': duration,
            'sample_rate': sr,
            'error': None
        }
        
    except Exception as e:
        return {
            'success': False,
            'error': f'Error extracting embeddings: {str(e)}',
            'embeddings': [],
            'shape': [],
            'model': model_name,
            'duration': 0,
            'sample_rate': 0
        }

def main():
    """Main function to handle command line arguments and execute extraction"""
    try:
        if len(sys.argv) < 2:
            print(json.dumps({
                'success': False,
                'error': 'Usage: python embedding_wrapper.py <audio_file_path> [model_name]',
                'embeddings': [],
                'shape': [],
                'model': '',
                'duration': 0,
                'sample_rate': 0
            }))
            sys.exit(1)
        
        audio_path = sys.argv[1]
        model_name = sys.argv[2] if len(sys.argv) > 2 else 'spectrogram'
        
        # Validate model name
        valid_models = ['spectrogram', 'openl3', 'audioclip']
        if model_name not in valid_models:
            print(json.dumps({
                'success': False,
                'error': f'Invalid model name: {model_name}. Valid options: {valid_models}',
                'embeddings': [],
                'shape': [],
                'model': model_name,
                'duration': 0,
                'sample_rate': 0
            }))
            sys.exit(1)
        
        # Extract embeddings
        result = extract_embeddings(audio_path, model_name)
        
        # Output result as JSON
        print(json.dumps(result))
        
    except Exception as e:
        # Handle any unexpected errors
        error_result = {
            'success': False,
            'error': f'Unexpected error: {str(e)}',
            'embeddings': [],
            'shape': [],
            'model': '',
            'duration': 0,
            'sample_rate': 0
        }
        print(json.dumps(error_result))
        sys.exit(1)

if __name__ == "__main__":
    main()
