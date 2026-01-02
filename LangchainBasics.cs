# topics_tutor.py

import os
from dotenv import load_dotenv
from langchain_google_genai import ChatGoogleGenerativeAI
from langchain_core.prompts import ChatPromptTemplate
from langchain_core.output_parsers import StrOutputParser

# 1. Load env and API key
load_dotenv()
api_key = os.getenv("GOOGLE_API_KEY")
if not api_key:
    raise ValueError("GOOGLE_API_KEY is not set in .env")

# 2. Define the LLM (Gemini)
model = ChatGoogleGenerativeAI(
    model="gemini-2.5-flash",
    temperature=0.7,
)

# 3. Define the topics you want to teach
TOPICS = {
    "prompt_engineering": "Basics of prompts, instructions, and system messages.",
    "chains": "What are chains in LangChain and why they are useful.",
    "prompt_templates": "Using ChatPromptTemplate and variables in prompts.",
    "memory": "Conversation memory and when to use it.",
    "tools_agents": "Using tools and agents conceptually in LangChain.",
    "rag_basics": "Retrieval-augmented generation at a high level.",
}

# 4. Create a reusable prompt template
prompt = ChatPromptTemplate.from_messages(
    [
        (
            "system",
            (
                "You are a GenAI instructor teaching LangChain to beginners. "
                "Explain clearly, with examples, and keep it practical."
            )
        ),
        (
            "user",
            (
                "Topic: {topic_name}\n"
                "Topic description: {topic_description}\n"
                "Explain this topic for a {level} learner.\n"
                "Format: {style}.\n"
                "Also give 1 small practical example related to Python + LangChain."
            ),
        ),
    ]
)

# 5. Build the chain
chain = prompt | model | StrOutputParser()


def explain_topic(topic_key: str, level: str = "beginner", style: str = "short notes"):
    """Run the LangChain pipeline for a given topic."""
    if topic_key not in TOPICS:
        raise ValueError(f"Unknown topic: {topic_key}")
    print("I am invoking the langchain topics.")
    return chain.invoke(
        {
            "topic_name": topic_key.replace("_", " ").title(),
            "topic_description": TOPICS[topic_key],
            "level": level,
            "style": style,
        }
    )


def print_menu():
    print("\n=== LangChain Topic Tutor ===")
    print("Choose a topic:")
    for i, key in enumerate(TOPICS.keys(), start=1):
        print(f"{i}. {key.replace('_', ' ').title()}")
    print("0. Exit")


def main():
    while True:
        print_menu()
        print("I am currently in the main menu.")
        choice = input("Enter your choice: ").strip()

        if choice == "0":
            print("Bye! 👋")
            break

        # Map numeric choice -> topic key
        try:
            idx = int(choice)
            print("currently I am here.")
            topic_key = list(TOPICS.keys())[idx - 1]
        except (ValueError, IndexError):
            print("Invalid choice, try again.")
            continue

        level = input("Level (beginner/intermediate/advanced) [beginner]: ").strip() or "beginner"
        style = input("Style (short notes/detailed explanation/bullet points) [short notes]: ").strip() or "short notes"

        print("\nGenerating explanation... ⏳\n")
        content = explain_topic(topic_key, level=level, style=style)
        print(content)
        print("\n" + "=" * 60 + "\n")


if __name__ == "__main__":
    main()
