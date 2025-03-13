use anyhow::Result;
use std::path::PathBuf;
use std::thread;
use std::time::Duration;
use sysinfo::Signal;
use tracing::info;

use lancelot::{
    agent::{BasicAgent, EvolvableAgent},
    sapi::{
        SystemCallRequest, ProcessOperation, ProcessFilter, SearchCriteria,
        system_call, clear_message_queue, get_queue_size, peek_next_message
    },
    cms::CodeModification,
};

fn main() -> Result<()> {
    // Initialize logging
    tracing_subscriber::fmt()
        .with_env_filter(tracing_subscriber::EnvFilter::from_default_env())
        .init();

    // Create a new agent for process monitoring
    let mut agent = BasicAgent::new("process_monitor_agent".to_string(), PathBuf::from("src"));

    // Initial system call should fail (needs evolution)
    let list_request = SystemCallRequest::new("list_processes".to_string(), 0)
        .with_operation(ProcessOperation::List);
    
    let result = agent.system_call(&list_request.command);
    println!("Initial system call result: {:?}", result);
    assert!(result.is_err(), "Initial system call should fail before evolution");

    // Evolve the agent
    assert!(agent.evolve()?);

    // Now demonstrate all the new features:

    // 1. Process Filtering
    println!("\n=== Process Filtering ===");
    let filter = ProcessFilter {
        min_cpu: Some(1.0),
        min_memory: Some(1024 * 1024), // 1MB
        name_pattern: Some("chrome".to_string()),
        status: None,
        running_time: Some(60), // At least 1 minute running
    };

    let filter_request = SystemCallRequest::new("filter_processes".to_string(), 1)
        .with_operation(ProcessOperation::Filter(filter));
    let filter_result = agent.system_call(&filter_request.command)?;
    println!("Filtered Processes:\n{}", filter_result);

    // 2. Process Searching
    println!("\n=== Process Searching ===");
    let search = SearchCriteria {
        name_contains: Some("system".to_string()),
        command_contains: None,
        user_name: None,
        max_results: Some(5),
    };

    let search_request = SystemCallRequest::new("search_processes".to_string(), 1)
        .with_operation(ProcessOperation::Search(search));
    let search_result = agent.system_call(&search_request.command)?;
    println!("Search Results:\n{}", search_result);

    // 3. Enhanced IPC
    println!("\n=== Enhanced IPC ===");
    let pid = std::process::id() as usize;

    // Clear any existing messages
    clear_message_queue(pid)?;

    // Send messages with different priorities
    let messages = vec![
        "Low priority message",
        "Normal priority message",
        "High priority message",
        "Critical priority message",
    ];

    for msg in messages {
        let send_request = SystemCallRequest::new("send_message".to_string(), 1)
            .with_operation(ProcessOperation::SendMessage(pid, msg.to_string()));
        agent.system_call(&send_request.command)?;
    }

    // Check queue size
    println!("Message queue size: {}", get_queue_size(pid)?);

    // Peek at next message
    if let Some(msg) = peek_next_message(pid)? {
        println!("Next message: {:?}", msg);
    }

    // Receive all messages
    while get_queue_size(pid)? > 0 {
        let receive_request = SystemCallRequest::new("receive_message".to_string(), 1)
            .with_operation(ProcessOperation::ReceiveMessage(pid));
        let receive_result = agent.system_call(&receive_request.command)?;
        println!("Received: {}", receive_result);
        thread::sleep(Duration::from_millis(100));
    }

    // 4. Resource Monitoring
    println!("\n=== Resource Monitoring ===");
    let monitor_request = SystemCallRequest::new("monitor_process".to_string(), 1)
        .with_operation(ProcessOperation::Monitor(pid.to_string()));

    for _ in 0..3 {
        let monitor_result = agent.system_call(&monitor_request.command)?;
        println!("\nMonitoring Result:\n{}", monitor_result);
        thread::sleep(Duration::from_secs(1));
    }

    println!("\nProcess monitor example completed successfully");
    Ok(())
} 