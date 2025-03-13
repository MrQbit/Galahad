use anyhow::Result;
use lancelot::model::{ModelManager, ModelManagerConfig};
use lancelot::agent::{BasicAgent, EvolvableAgent};
use std::path::PathBuf;
use clap::Parser;
use tracing::{info, Level};
use tracing_subscriber::FmtSubscriber;

#[derive(Parser)]
#[command(author, version, about, long_about = None)]
struct Cli {
    /// The prompt to generate from
    #[arg(short, long)]
    prompt: String,

    /// Which model to use (tinyllama or codellama)
    #[arg(short, long)]
    model: String,

    /// Log level (trace, debug, info, warn, error)
    #[arg(short, long, default_value = "debug")]
    log_level: String,

    /// Device to use (cpu, metal, or cuda)
    #[arg(short, long, default_value = "metal")]
    device: String,
}

fn main() -> Result<()> {
    // Parse command line arguments
    let cli = Cli::parse();

    // Initialize logging with specified level
    let level = match cli.log_level.to_lowercase().as_str() {
        "trace" => Level::TRACE,
        "debug" => Level::DEBUG,
        "info" => Level::INFO,
        "warn" => Level::WARN,
        "error" => Level::ERROR,
        _ => Level::INFO,
    };

    let subscriber = FmtSubscriber::builder()
        .with_max_level(level)
        .with_target(false)
        .with_thread_ids(true)
        .with_file(true)
        .with_line_number(true)
        .pretty()
        .init();

    info!("Starting model generation example");

    // Create and evolve agent to required stage
    let mut agent = BasicAgent::new("model_agent".to_string(), PathBuf::from("src"));
    
    // Evolve agent to code modification stage
    info!("Evolving agent to required stage...");
    while agent.evolution_stage() != lancelot::agent::EvolutionStage::CodeModification {
        if !agent.evolve()? {
            return Err(anyhow::anyhow!("Failed to evolve agent to required stage"));
        }
    }
    info!("Agent evolved successfully");

    // Create model manager config
    let model_path = format!("models/{}/{}.gguf", cli.model, cli.model);
    info!("Loading model from {}", model_path);
    
    let config = ModelManagerConfig {
        model_path,
        device: cli.device,
        context_size: 32,
        batch_size: 1,
    };

    // Initialize model manager
    let mut manager = ModelManager::new(config)?;
    manager.load_models()?;

    // Format the prompt based on the model
    let formatted_prompt = match cli.model.as_str() {
        "tinyllama" => format!("### Instruction: {}\n\n### Response:", cli.prompt),
        "codellama" => format!("### System: You are a helpful coding assistant. Write clean, efficient, and well-documented code.\n\n### User: {}\n\n### Assistant:", cli.prompt),
        _ => return Err(anyhow::anyhow!("Invalid model selection. Use 'tinyllama' or 'codellama'")),
    };

    // Generate based on selected model
    info!("Generating response for prompt: {}", formatted_prompt);
    let response = match cli.model.as_str() {
        "tinyllama" => manager.generate(&formatted_prompt)?,
        "codellama" => manager.generate_code(&formatted_prompt)?,
        _ => return Err(anyhow::anyhow!("Invalid model selection. Use 'tinyllama' or 'codellama'")),
    };

    println!("\nGenerated response:\n{}", response);

    Ok(())
} 