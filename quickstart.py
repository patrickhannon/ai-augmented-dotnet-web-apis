import os

from openai import OpenAI



endpoint = "https://llm-foundry-pjh-eastus2.services.ai.azure.com/openai/v1"
deployment_name = "gpt-4.1-mini"

api_key = os.environ["FOUNDRY_API_KEY"]


print(f"api_key: {api_key}")

client = OpenAI(
    base_url=endpoint,
    api_key=api_key
)

response = client.responses.create(
    model=deployment_name,
    input="What is the capital of England?",
)

print(f"answer: {response.output[0]}")
