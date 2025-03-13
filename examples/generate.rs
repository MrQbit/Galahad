use anyhow::Result;
use lancelot::{
    agent::{BasicAgent, EvolvableAgent},
    cms::CodeModification,
};
use std::path::PathBuf;

fn main() -> Result<()> {
    // Create a new agent
    let mut agent = BasicAgent::new("generator_agent".to_string(), PathBuf::from("src"));

    // Create a code modification
    let modification = CodeModification::new(
        PathBuf::from("test.rs"),
        "fn test() {}".to_string(),
        "Test modification".to_string(),
    );

    // Propose the modification
    agent.propose_modification(modification)?;

    Ok(())
} 