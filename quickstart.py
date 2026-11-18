import os
from openai import OpenAI

#print(f"FOUNDRY_RESOURCE_ENDPOINT: {os.environ['FOUNDRY_RESOURCE_ENDPOINT']}")
#print(f"FOUNDRY_API_KEY: {os.environ["FOUNDRY_API_KEY"]}")

client = OpenAI(
    base_url=f"{os.environ['FOUNDRY_RESOURCE_ENDPOINT']}/openai/v1/",
    api_key=os.environ["FOUNDRY_API_KEY"],
)

response = client.responses.create(
    model="model-router",
    input="Explain retrieval-augmented generation in two sentences.",
)

print(f"Model: {response.model}")
print(response.output_text)