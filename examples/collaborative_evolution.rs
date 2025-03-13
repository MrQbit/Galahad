use lancelot::{
    agent::{BasicAgent, EvolvableAgent},
};
use anyhow::Result;
use std::path::PathBuf;

fn main() -> Result<()> {
    // Create a new agent
    let mut agent = BasicAgent::new("collaborative_agent".to_string(), PathBuf::from("src"));

    // Initial system call should fail
    let result = agent.system_call("list_files");
    assert!(result.is_err());

    // Evolve the agent
    assert!(agent.evolve()?);

    // Now system call should succeed
    let result = agent.system_call("list_files");
    assert!(result.is_ok());

    Ok(())
} 