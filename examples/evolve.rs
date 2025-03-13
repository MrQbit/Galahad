use anyhow::Result;
use std::path::PathBuf;
use lancelot::{
    agent::{BasicAgent, EvolvableAgent},
    cms::CodeModification,
    sapi::SystemCallRequest,
};
use tracing::info;

fn main() -> Result<()> {
    // Initialize logging
    tracing_subscriber::fmt()
        .with_env_filter(tracing_subscriber::EnvFilter::from_default_env())
        .init();

    // Create a new agent
    let mut agent = BasicAgent::new("evolving_agent".to_string(), PathBuf::from("src"));
    info!("Created new agent: {:?}", agent.id());

    // Test initial stage - should fail all privileged operations
    info!("\nTesting Initial Stage:");
    let sys_call = SystemCallRequest::new("list_processes".to_string(), 0);
    let result = agent.system_call(&sys_call.command);
    assert!(result.is_err(), "System call should fail in initial stage");

    let code_mod = CodeModification::new(
        PathBuf::from("test.rs"),
        "fn test() {}".to_string(),
        "Test modification".to_string(),
    );
    let result = agent.propose_modification(code_mod.clone());
    assert!(result.is_err(), "Code modification should fail in initial stage");

    // Evolve to SystemAccess stage
    info!("\nEvolving to SystemAccess stage...");
    assert!(agent.evolve()?);
    info!("Current stage: {:?}", agent.evolution_stage());

    // Test SystemAccess stage
    let result = agent.system_call(&sys_call.command);
    assert!(result.is_ok(), "System call should succeed in SystemAccess stage");
    info!("System call result: {:?}", result);

    let result = agent.propose_modification(code_mod.clone());
    assert!(result.is_err(), "Code modification should still fail in SystemAccess stage");

    // Evolve to CodeModification stage
    info!("\nEvolving to CodeModification stage...");
    assert!(agent.evolve()?);
    info!("Current stage: {:?}", agent.evolution_stage());

    // Test CodeModification stage
    let result = agent.system_call(&sys_call.command);
    assert!(result.is_ok(), "System call should still work in CodeModification stage");

    let result = agent.propose_modification(code_mod.clone());
    assert!(result.is_ok(), "Code modification should succeed in CodeModification stage");
    info!("Code modification result: {:?}", result);

    // Evolve to Advanced stage
    info!("\nEvolving to Advanced stage...");
    assert!(agent.evolve()?);
    info!("Current stage: {:?}", agent.evolution_stage());

    // Test Advanced stage capabilities
    let result = agent.system_call(&sys_call.command);
    assert!(result.is_ok(), "System call should work in Advanced stage");

    let result = agent.propose_modification(code_mod);
    assert!(result.is_ok(), "Code modification should work in Advanced stage");

    let result = agent.hardware_operation(lancelot::hal::HardwareOperation::QueryCapabilities);
    assert!(result.is_ok(), "Hardware operation should work in Advanced stage");
    info!("Hardware operation result: {:?}", result);

    info!("\nEvolution example completed successfully");
    Ok(())
}