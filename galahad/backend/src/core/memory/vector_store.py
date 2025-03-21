from qdrant_client import QdrantClient
from qdrant_client.http import models
from uuid import uuid4

class VectorStore:
    def __init__(self, host="localhost", port=6333):
        self.client = QdrantClient(host=host, port=port)
        
    async def init_collections(self):
        """Initialize required collections"""
        collections = {
            "agent_memory": 1536,  # OpenAI embedding size
            "context_memory": 1536,
            "tool_memory": 1536
        }
        
        for name, size in collections.items():
            await self.client.create_collection(
                collection_name=name,
                vectors_config=models.VectorParams(
                    size=size,
                    distance=models.Distance.COSINE
                )
            )

    async def store_vector(self, collection: str, vector: list[float], metadata: dict):
        """Store a vector with metadata"""
        return await self.client.upsert(
            collection_name=collection,
            points=[
                models.PointStruct(
                    id=str(uuid4()),
                    vector=vector,
                    payload=metadata
                )
            ]
        )
