#!/usr/bin/env python3
"""
Vector Search Wrapper Script

This script provides a bridge between the .NET Core backend and the vector-search Python module.
It performs similarity search using FAISS and returns results in JSON format.

Created by: Sergie Code - Software Engineer & YouTube Programming Educator
AI Tools for Musicians Series
"""

import sys
import json
import os
import traceback
from pathlib import Path
import numpy as np

# Add the vector-search module to the Python path
VECTOR_SEARCH_REPO_PATH = os.path.join(os.path.dirname(__file__), "..", "..", "copyright-detector-vector-search")
sys.path.insert(0, os.path.join(VECTOR_SEARCH_REPO_PATH, "src"))

try:
    # Import from the vector-search module
    from indexer import VectorIndexer
    from search import SimilaritySearcher, CopyrightDetector
except ImportError as e:
    # Fallback if modules are not available
    print(json.dumps({
        'success': False,
        'error': f'Failed to import vector search modules: {str(e)}',
        'similarTracks': [],
        'copyrightRisk': 'UNKNOWN',
        'riskScore': 0.0,
        'totalMatches': 0,
        'processingTimeMs': 0
    }))
    sys.exit(1)

def search_similar_tracks(embedding_data, index_path, top_k=10, similarity_threshold=0.8):
    """
    Search for similar tracks using FAISS vector search
    
    Args:
        embedding_data (str): JSON string containing embedding array
        index_path (str): Path to the FAISS index file
        top_k (int): Number of similar tracks to return
        similarity_threshold (float): Threshold for copyright detection
    
    Returns:
        dict: Search results with similar tracks and copyright analysis
    """
    try:
        # Parse embedding data
        try:
            embeddings = json.loads(embedding_data)
            query_embedding = np.array(embeddings, dtype=np.float32)
        except (json.JSONDecodeError, ValueError) as e:
            return {
                'success': False,
                'error': f'Invalid embedding data: {str(e)}',
                'similarTracks': [],
                'copyrightRisk': 'UNKNOWN',
                'riskScore': 0.0,
                'totalMatches': 0,
                'processingTimeMs': 0
            }
        
        # Check if index file exists
        if not os.path.exists(index_path):
            return {
                'success': False,
                'error': f'Index file not found: {index_path}',
                'similarTracks': [],
                'copyrightRisk': 'UNKNOWN',
                'riskScore': 0.0,
                'totalMatches': 0,
                'processingTimeMs': 0
            }
        
        # Load the search engine
        try:
            searcher = SimilaritySearcher(index_path=index_path)
            detector = CopyrightDetector(searcher)
        except Exception as e:
            return {
                'success': False,
                'error': f'Failed to load search index: {str(e)}',
                'similarTracks': [],
                'copyrightRisk': 'UNKNOWN',
                'riskScore': 0.0,
                'totalMatches': 0,
                'processingTimeMs': 0
            }
        
        # Perform similarity search
        try:
            # Get similar tracks
            similar_results = searcher.search_similar(query_embedding, k=top_k)
            
            # Perform copyright analysis
            copyright_analysis = detector.analyze_embedding(query_embedding)
            
            # Convert results to the expected format
            similar_tracks = []
            for result in similar_results:
                similar_track = {
                    'filename': result.get('filename', ''),
                    'artist': result.get('artist', ''),
                    'album': result.get('album', ''),
                    'genre': result.get('genre', ''),
                    'similarityScore': float(result.get('similarity_score', 0.0)),
                    'distance': float(result.get('distance', 0.0)),
                    'copyrightRisk': calculate_track_risk(result.get('similarity_score', 0.0), similarity_threshold),
                    'duration': float(result.get('duration', 0.0)),
                    'year': result.get('year')
                }
                similar_tracks.append(similar_track)
            
            # Calculate overall risk
            overall_risk = copyright_analysis.get('overall_risk', 'LOW')
            risk_score = copyright_analysis.get('risk_score', 0.0)
            
            return {
                'success': True,
                'similarTracks': similar_tracks,
                'copyrightRisk': overall_risk,
                'riskScore': float(risk_score),
                'totalMatches': len(similar_tracks),
                'processingTimeMs': 0,  # Will be calculated by the calling service
                'error': None
            }
            
        except Exception as e:
            return {
                'success': False,
                'error': f'Search operation failed: {str(e)}',
                'similarTracks': [],
                'copyrightRisk': 'UNKNOWN',
                'riskScore': 0.0,
                'totalMatches': 0,
                'processingTimeMs': 0
            }
        
    except Exception as e:
        return {
            'success': False,
            'error': f'Unexpected error during search: {str(e)}',
            'similarTracks': [],
            'copyrightRisk': 'UNKNOWN',
            'riskScore': 0.0,
            'totalMatches': 0,
            'processingTimeMs': 0
        }

def calculate_track_risk(similarity_score, threshold):
    """Calculate copyright risk level for a single track"""
    if similarity_score >= 0.95:
        return "VERY_HIGH"
    elif similarity_score >= 0.85:
        return "HIGH"
    elif similarity_score >= threshold:
        return "MEDIUM"
    elif similarity_score >= 0.5:
        return "LOW"
    else:
        return "VERY_LOW"

def create_demo_index_if_missing(index_path):
    """Create a demo index if the specified index doesn't exist"""
    try:
        if os.path.exists(index_path):
            return True
            
        # Create a simple demo index for testing
        indexer = VectorIndexer(dimension=128, index_type="FlatL2")
        
        # Generate some random demo embeddings
        demo_embeddings = np.random.rand(100, 128).astype(np.float32)
        demo_metadata = [
            {
                'filename': f'demo_song_{i}.wav',
                'artist': f'Demo Artist {i//10}',
                'album': f'Demo Album {i//20}',
                'genre': ['Rock', 'Pop', 'Jazz'][i % 3],
                'duration': 180.0 + (i % 60),
                'year': 2020 + (i % 5)
            }
            for i in range(100)
        ]
        
        indexer.add_embeddings(demo_embeddings, demo_metadata)
        
        # Create directory if it doesn't exist
        os.makedirs(os.path.dirname(index_path), exist_ok=True)
        
        # Save the demo index
        indexer.save_index(index_path)
        
        return True
        
    except Exception as e:
        print(f"Warning: Could not create demo index: {str(e)}", file=sys.stderr)
        return False

def main():
    """Main function to handle command line arguments and execute search"""
    try:
        if len(sys.argv) < 3:
            print(json.dumps({
                'success': False,
                'error': 'Usage: python search_wrapper.py <embedding_json> <index_path> [top_k] [similarity_threshold]',
                'similarTracks': [],
                'copyrightRisk': 'UNKNOWN',
                'riskScore': 0.0,
                'totalMatches': 0,
                'processingTimeMs': 0
            }))
            sys.exit(1)
        
        embedding_data = sys.argv[1]
        index_path = sys.argv[2]
        top_k = int(sys.argv[3]) if len(sys.argv) > 3 else 10
        similarity_threshold = float(sys.argv[4]) if len(sys.argv) > 4 else 0.8
        
        # Validate parameters
        if top_k <= 0 or top_k > 100:
            print(json.dumps({
                'success': False,
                'error': 'top_k must be between 1 and 100',
                'similarTracks': [],
                'copyrightRisk': 'UNKNOWN',
                'riskScore': 0.0,
                'totalMatches': 0,
                'processingTimeMs': 0
            }))
            sys.exit(1)
        
        if similarity_threshold < 0.0 or similarity_threshold > 1.0:
            print(json.dumps({
                'success': False,
                'error': 'similarity_threshold must be between 0.0 and 1.0',
                'similarTracks': [],
                'copyrightRisk': 'UNKNOWN',
                'riskScore': 0.0,
                'totalMatches': 0,
                'processingTimeMs': 0
            }))
            sys.exit(1)
        
        # Create demo index if it doesn't exist (for testing purposes)
        if not os.path.exists(index_path):
            create_demo_index_if_missing(index_path)
        
        # Perform the search
        result = search_similar_tracks(embedding_data, index_path, top_k, similarity_threshold)
        
        # Output result as JSON
        print(json.dumps(result))
        
    except Exception as e:
        # Handle any unexpected errors
        error_result = {
            'success': False,
            'error': f'Unexpected error: {str(e)}',
            'similarTracks': [],
            'copyrightRisk': 'UNKNOWN',
            'riskScore': 0.0,
            'totalMatches': 0,
            'processingTimeMs': 0
        }
        print(json.dumps(error_result))
        sys.exit(1)

if __name__ == "__main__":
    main()
